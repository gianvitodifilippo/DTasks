using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;
using DTasks.Execution;
using DTasks.Infrastructure.DependencyInjection;
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
    private readonly IDAsyncInfrastructure _infrastructure;

#if DEBUG
    private string? _stackTrace;
#endif

    private readonly HostComponentProvider _hostComponentProvider;
    private readonly FlowComponentProvider _flowComponentProvider;
    private IDAsyncHost _host;
    private IDAsyncStack? _stack;
    private IDAsyncHeap? _heap;
    private IDAsyncSurrogator? _surrogator;
    private IDAsyncCancellationProvider? _cancellationProvider;
    private IDAsyncSuspensionHandler? _suspensionHandler;

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

    private bool _clearFlowProperties;
    private Dictionary<object, object?>? _flowProperties;

    private DAsyncFlow(IDAsyncFlowPool pool, IDAsyncInfrastructure infrastructure)
    {
        _pool = pool;
        _infrastructure = infrastructure;
        _hostComponentProvider = infrastructure.RootProvider.CreateHostProvider(this);
        _flowComponentProvider = _hostComponentProvider.CreateFlowProvider(this);
        _state = FlowState.Idling;
        _builder = AsyncTaskMethodBuilder.Create();
        _host = s_nullHost;
        _taskSurrogateConverter = new DTaskSurrogateConverter(this);
        // TODO: Lazily initialize these
        _surrogates = [];
        _tasks = [];
        _cancellationInfos = [];
        _cancellations = [];
    }

    protected override IDAsyncHostInfrastructure InfrastructureCore => _hostComponentProvider;

    private IDAsyncTypeResolver TypeResolver => _infrastructure.TypeResolver;

    private IDAsyncStack Stack => _stack ??= _infrastructure.GetStack(_flowComponentProvider);

    private IDAsyncHeap Heap => _heap ??= _infrastructure.GetHeap(_flowComponentProvider);

    private IDAsyncSurrogator Surrogator => _surrogator ??= _infrastructure.GetSurrogator(_flowComponentProvider);

    private IDAsyncCancellationProvider CancellationProvider => _cancellationProvider ??= _infrastructure.GetCancellationProvider(_flowComponentProvider);

    private IDAsyncSuspensionHandler SuspensionHandler => _suspensionHandler ??= _infrastructure.GetSuspensionHandler(_flowComponentProvider);

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
    public void Initialize(IDAsyncHost host, string? stackTrace)
    {
        _state = FlowState.Pending;
        _host = host;
        _stackTrace = stackTrace;
    }
#else
    public void Initialize(IDAsyncHost host)
    {
        _state = FlowState.Pending;
        _host = host;
    }
#endif

    public new ValueTask StartAsync(IDAsyncRunnable runnable, CancellationToken cancellationToken)
    {
        return StartCoreAsync(runnable, cancellationToken);
    }

    public new ValueTask ResumeAsync(DAsyncId id, CancellationToken cancellationToken)
    {
        return ResumeCoreAsync(id, cancellationToken);
    }

    public new ValueTask ResumeAsync<TResult>(DAsyncId id, TResult result, CancellationToken cancellationToken)
    {
        return ResumeCoreAsync(id, result, cancellationToken);
    }

    public new ValueTask ResumeAsync(DAsyncId id, Exception exception, CancellationToken cancellationToken)
    {
        return ResumeCoreAsync(id, exception, cancellationToken);
    }

    protected override ValueTask StartCoreAsync(IDAsyncRunnable runnable, CancellationToken cancellationToken)
    {
        if (_state is not FlowState.Pending)
            throw new InvalidOperationException($"Detected invalid usage of {nameof(DAsyncFlow)}");

        _runnable = runnable;
        _cancellationToken = cancellationToken;
        _parentId = DAsyncId.NewFlowId(); // TODO: Move after OnStartAsync
        _id = DAsyncId.New(); // TODO: Move after OnStartAsync

        InitializeFlow(); // TODO: Move after OnStartAsync
        AwaitOnStart();
        
        return new ValueTask(this, _valueTaskSource.Version);
    }

    protected override ValueTask ResumeCoreAsync(DAsyncId id, CancellationToken cancellationToken)
    {
        if (_state is not FlowState.Pending)
            throw new InvalidOperationException($"Detected invalid usage of {nameof(DAsyncFlow)}");

        _state = FlowState.Running;
        _cancellationToken = cancellationToken;

        InitializeFlow();
        Resume(id);
        
        return new ValueTask(this, _valueTaskSource.Version);
    }

    protected override ValueTask ResumeCoreAsync<TResult>(DAsyncId id, TResult result, CancellationToken cancellationToken)
    {
        if (_state is not FlowState.Pending)
            throw new InvalidOperationException($"Detected invalid usage of {nameof(DAsyncFlow)}");

        _state = FlowState.Running;
        _cancellationToken = cancellationToken;
        
        InitializeFlow();
        Resume(id, result);

        return new ValueTask(this, _valueTaskSource.Version);
    }

    protected override ValueTask ResumeCoreAsync(DAsyncId id, Exception exception, CancellationToken cancellationToken)
    {
        if (_state is not FlowState.Pending)
            throw new InvalidOperationException($"Detected invalid usage of {nameof(DAsyncFlow)}");

        _state = FlowState.Running;
        _cancellationToken = cancellationToken;

        InitializeFlow();
        Resume(id, exception);
        
        return new ValueTask(this, _valueTaskSource.Version);
    }

    private void InitializeFlow()
    {
        _clearFlowProperties = true;
        _flowComponentProvider.BeginScope();
        _host.OnInitialize(this);
        CancellationProvider.RegisterHandler(this);
    }

    [DebuggerStepThrough]
    private static T Consume<T>([MaybeNull] ref T value)
    {
        T result = value;
        value = default;
        return result;
    }

    public static DAsyncFlow Create(IDAsyncFlowPool pool, IDAsyncInfrastructure infrastructure)
    {
        DAsyncFlow flow = new(pool, infrastructure);

#if DEBUG
        GC.ReRegisterForFinalize(flow);
#endif

        return flow;
    }
}
