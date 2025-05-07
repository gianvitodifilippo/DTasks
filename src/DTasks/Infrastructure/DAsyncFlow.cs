using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;
using DTasks.Configuration;
using DTasks.Execution;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : DAsyncRunner
{
    private FlowState _state;
    private AsyncTaskMethodBuilder _builder;
    private ManualResetValueTaskSourceCore<VoidDTaskResult> _valueTaskSource;
    private CancellationToken _cancellationToken;
    private object? _resultOrException;

    private readonly IDAsyncFlowPool _pool;
    private bool _returnToPool;

#if DEBUG
    private string? _stackTrace;
#endif

    private IDAsyncInfrastructure _infrastructure;
    private IDAsyncHost _host;
    private IDAsyncStack? _stack;

    private TaskAwaiter _voidTa;
    private ValueTaskAwaiter _voidVta;
    private ValueTaskAwaiter<DAsyncLink> _linkVta;

    private DAsyncId _parentId;
    private DAsyncId _id;
    private DAsyncId _childId;
    private IDAsyncRunnable? _runnable;
    private IDAsyncStateMachine? _stateMachine;
    private object? _suspendingAwaiterOrType;
    private TimeSpan? _delay;
    private ISuspensionCallback? _callback;
    private AggregateType _aggregateType;
    private IEnumerable<IDAsyncRunnable>? _aggregateBranches;
    private List<Exception>? _aggregateExceptions;
    private int _branchCount;
    private IDictionary? _whenAllBranchResults;
    private IDAsyncRunnable? _aggregateRunnable;
    private Task? _awaitedTask;
    private object? _resultBuilder;
    private Type? _handleResultType;
    private FlowContinuation? _continuation;

    private DAsyncFlow? _parent;
    private int _branchIndex = -1;

    private readonly DTaskSurrogateConverter _taskSurrogateConverter;
    private readonly Dictionary<DTask, DTaskSurrogate> _surrogates;
    private readonly Dictionary<DAsyncId, DTask> _tasks;
    private readonly ConcurrentDictionary<DCancellationTokenSource, CancellationInfo> _cancellationInfos;
    private readonly ConcurrentDictionary<DCancellationId, DCancellationTokenSource> _cancellations;

    private Dictionary<Type, object?>? _features;

    private DAsyncFlow(IDAsyncFlowPool pool)
    {
        _pool = pool;
        _state = FlowState.Idling;
        _builder = AsyncTaskMethodBuilder.Create();
        _infrastructure = s_nullInfrastructure;
        _host = s_nullHost;
        _taskSurrogateConverter = new DTaskSurrogateConverter(this);
        _surrogates = [];
        _tasks = [];
        _cancellationInfos = [];
        _cancellations = [];
        _features = [];
    }

    private IDAsyncTypeResolver TypeResolver => _infrastructure.TypeResolver;

    private IDAsyncStack Stack => _stack ??= _infrastructure.GetStack(this);

    private IDAsyncHeap Heap => _infrastructure.Heap;

    private IDAsyncSurrogator Surrogator => _infrastructure.Surrogator;

    private IDAsyncCancellationProvider CancellationProvider => _infrastructure.CancellationProvider;

    private IDAsyncSuspensionHandler SuspensionHandler => _infrastructure.SuspensionHandler;

    [MemberNotNullWhen(true, nameof(_parent))]
    private bool IsRunningAggregates => _parent is not null;

    private bool IsWhenAllResultBranch
    {
        get
        {
            bool result = _branchIndex != -1;
            Debug.Assert(!result || IsRunningAggregates && _parent._aggregateType is AggregateType.WhenAllResult, $"'{_branchIndex}' should be set only when running branches of a WhenAll runnable.");
            return result;
        }
    }

#if DEBUG
    public void Initialize(IDAsyncInfrastructure infrastructure, string? stackTrace)
    {
        _infrastructure = infrastructure;
        _stackTrace = stackTrace;
        _returnToPool = stackTrace is null;
    }
#else
    public void Initialize(IDAsyncInfrastructure infrastructure, bool returnToPool)
    {
        _infrastructure = infrastructure;
        _returnToPool = returnToPool;
    }
#endif

    public new ValueTask StartAsync(IDAsyncHost host, IDAsyncRunnable runnable, CancellationToken cancellationToken)
    {
        return StartCoreAsync(host, runnable, cancellationToken);
    }

    public new ValueTask ResumeAsync(IDAsyncHost host, DAsyncId id, CancellationToken cancellationToken)
    {
        return ResumeCoreAsync(host, id, cancellationToken);
    }

    public new ValueTask ResumeAsync<TResult>(IDAsyncHost host, DAsyncId id, TResult result, CancellationToken cancellationToken)
    {
        return ResumeCoreAsync(host, id, result, cancellationToken);
    }

    public new ValueTask ResumeAsync(IDAsyncHost host, DAsyncId id, Exception exception, CancellationToken cancellationToken)
    {
        return ResumeCoreAsync(host, id, exception, cancellationToken);
    }

    protected override ValueTask StartCoreAsync(IDAsyncHost host, IDAsyncRunnable runnable, CancellationToken cancellationToken)
    {
        if (_state is not FlowState.Pending)
            throw new InvalidOperationException($"Detected invalid usage of {nameof(DAsyncFlow)}");

        _runnable = runnable;
        _cancellationToken = cancellationToken;
        _parentId = DAsyncId.NewFlowId(); // TODO: Move after OnStartAsync
        _id = DAsyncId.New(); // TODO: Move after OnStartAsync

        BeginFlow(host); // TODO: Move after OnStartAsync
        AwaitOnStart();
        
        return new ValueTask(this, _valueTaskSource.Version);
    }

    protected override ValueTask ResumeCoreAsync(IDAsyncHost host, DAsyncId id, CancellationToken cancellationToken)
    {
        if (_state is not FlowState.Pending)
            throw new InvalidOperationException($"Detected invalid usage of {nameof(DAsyncFlow)}");

        _state = FlowState.Running;
        _cancellationToken = cancellationToken;

        BeginFlow(host);
        Resume(id);
        
        return new ValueTask(this, _valueTaskSource.Version);
    }

    protected override ValueTask ResumeCoreAsync<TResult>(IDAsyncHost host, DAsyncId id, TResult result, CancellationToken cancellationToken)
    {
        if (_state is not FlowState.Pending)
            throw new InvalidOperationException($"Detected invalid usage of {nameof(DAsyncFlow)}");

        _state = FlowState.Running;
        _cancellationToken = cancellationToken;
        
        BeginFlow(host);
        Resume(id, result);

        return new ValueTask(this, _valueTaskSource.Version);
    }

    protected override ValueTask ResumeCoreAsync(IDAsyncHost host, DAsyncId id, Exception exception, CancellationToken cancellationToken)
    {
        if (_state is not FlowState.Pending)
            throw new InvalidOperationException($"Detected invalid usage of {nameof(DAsyncFlow)}");

        _state = FlowState.Running;
        _cancellationToken = cancellationToken;

        BeginFlow(host);
        Resume(id, exception);
        
        return new ValueTask(this, _valueTaskSource.Version);
    }

    private void BeginFlow(IDAsyncHost host)
    {
        _host = host;

        host.OnInitialize(this);
        CancellationProvider.RegisterHandler(this);
    }

    [DebuggerStepThrough]
    private static T Consume<T>([MaybeNull] ref T value)
    {
        T result = value;
        value = default;
        return result;
    }

    public static DAsyncFlow Create(IDAsyncFlowPool pool)
    {
        DAsyncFlow flow = new(pool);

#if DEBUG
        GC.ReRegisterForFinalize(flow);
#endif

        return flow;
    }
}
