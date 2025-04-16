using DTasks.Execution;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using DTasks.Utils;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;

namespace DTasks.Infrastructure;

public sealed partial class DAsyncFlow
{
    private FlowState _state;
    private AsyncTaskMethodBuilder _builder;
    private ManualResetValueTaskSourceCore<VoidDTaskResult> _valueTaskSource;
    private CancellationToken _cancellationToken;
    private IDAsyncHost _host;
    private object? _resultOrException;
    
    private bool _returnToCache;
#if DEBUG
    private string? _stackTrace;
#endif

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

    private DAsyncFlow()
    {
        _state = FlowState.Idling;
        _builder = AsyncTaskMethodBuilder.Create();
        _host = s_nullHost;
        _taskSurrogateConverter = new DTaskSurrogateConverter(this);
        _surrogates = [];
        _tasks = [];
        _cancellationInfos = [];
        _cancellations = [];
        
#if DEBUG
        GC.SuppressFinalize(this);
#endif
    }

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

    public ValueTask StartAsync(IDAsyncHost host, IDAsyncRunnable runnable, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);
        ThrowHelper.ThrowIfNull(runnable);
        
        return StartCoreAsync(host, runnable, cancellationToken);
    }

    public ValueTask ResumeAsync(IDAsyncHost host, DAsyncId id, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);
        
        return ResumeCoreAsync(host, id, cancellationToken);
    }

    public ValueTask ResumeAsync<TResult>(IDAsyncHost host, DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);
        
        return ResumeCoreAsync(host, id, result, cancellationToken);
    }

    public ValueTask ResumeAsync(IDAsyncHost host, DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);
        ThrowHelper.ThrowIfNull(exception);

        return ResumeCoreAsync(host, id, exception, cancellationToken);
    }

    private void Initialize(IDAsyncHost host)
    {
        _host = host;
        _host.CancellationProvider.RegisterHandler(this);
    }
    
    private ValueTask StartCoreAsync(IDAsyncHost host, IDAsyncRunnable runnable, CancellationToken cancellationToken)
    {
        if (_state is not FlowState.Pending)
            throw new InvalidOperationException($"Detected invalid usage of {nameof(DAsyncFlow)}");

        _runnable = runnable;
        _cancellationToken = cancellationToken;
        _parentId = DAsyncId.NewFlowId();
        _id = DAsyncId.New();
        Initialize(host);

        AwaitOnStart();
        return new ValueTask(this, _valueTaskSource.Version);
    }

    private ValueTask ResumeCoreAsync(IDAsyncHost host, DAsyncId id, CancellationToken cancellationToken = default)
    {
        if (_state is not FlowState.Pending)
            throw new InvalidOperationException($"Detected invalid usage of {nameof(DAsyncFlow)}");

        _state = FlowState.Running;
        _cancellationToken = cancellationToken;
        Initialize(host);

        Resume(id);
        return new ValueTask(this, _valueTaskSource.Version);
    }

    private ValueTask ResumeCoreAsync<TResult>(IDAsyncHost host, DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        if (_state is not FlowState.Pending)
            throw new InvalidOperationException($"Detected invalid usage of {nameof(DAsyncFlow)}");

        _state = FlowState.Running;
        _cancellationToken = cancellationToken;
        Initialize(host);
        
        Resume(id, result);
        return new ValueTask(this, _valueTaskSource.Version);
    }

    private ValueTask ResumeCoreAsync(IDAsyncHost host, DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        if (_state is not FlowState.Pending)
            throw new InvalidOperationException($"Detected invalid usage of {nameof(DAsyncFlow)}");

        _state = FlowState.Running;
        _cancellationToken = cancellationToken;
        Initialize(host);

        Resume(id, exception);
        return new ValueTask(this, _valueTaskSource.Version);
    }

    [DebuggerStepThrough]
    private static T Consume<T>([MaybeNull] ref T value)
    {
        T result = value;
        value = default;
        return result;
    }

    public static DAsyncFlow Create()
    {
        DAsyncFlow flow = RentFromCache(returnToCache: false);
        
#if DEBUG
        GC.ReRegisterForFinalize(flow);
        flow._stackTrace = Environment.StackTrace;
#endif
        
        return flow;
    }
    
    public static ValueTask StartFlowAsync(IDAsyncHost host, IDAsyncRunnable runnable, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);
        ThrowHelper.ThrowIfNull(runnable);
        
        DAsyncFlow flow = RentFromCache(returnToCache: true);
        
        return flow.StartCoreAsync(host, runnable, cancellationToken);
    }
    
    public static ValueTask ResumeFlowAsync(IDAsyncHost host, DAsyncId id, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);
        
        DAsyncFlow flow = RentFromCache(returnToCache: true);
        return flow.ResumeCoreAsync(host, id, cancellationToken);
    }
    
    public static ValueTask ResumeFlowAsync<TResult>(IDAsyncHost host, DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);

        DAsyncFlow flow = RentFromCache(returnToCache: true);
        return flow.ResumeCoreAsync(host, id, result, cancellationToken);
    }
    
    public static ValueTask ResumeFlowAsync(IDAsyncHost host, DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(host);

        DAsyncFlow flow = RentFromCache(returnToCache: true);
        return flow.ResumeCoreAsync(host, id, exception, cancellationToken);
    }

    public static ValueTask StartFlowAsync(IDAsyncHostFactory hostFactory, IDAsyncRunnable runnable, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(hostFactory);
        ThrowHelper.ThrowIfNull(runnable);

        DAsyncFlow flow = RentFromCache(returnToCache: true);
        IDAsyncHost host = hostFactory.CreateHost(flow);
        return flow.StartCoreAsync(host, runnable, cancellationToken);
    }

    public static ValueTask ResumeFlowAsync(IDAsyncHostFactory hostFactory, DAsyncId id, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(hostFactory);

        DAsyncFlow flow = RentFromCache(returnToCache: true);
        IDAsyncHost host = hostFactory.CreateHost(flow);
        return flow.ResumeCoreAsync(host, id, cancellationToken);
    }

    public static ValueTask ResumeFlowAsync<TResult>(IDAsyncHostFactory hostFactory, DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(hostFactory);

        DAsyncFlow flow = RentFromCache(returnToCache: true);
        IDAsyncHost host = hostFactory.CreateHost(flow);
        return flow.ResumeCoreAsync(host, id, result, cancellationToken);
    }

    public static ValueTask ResumeFlowAsync(IDAsyncHostFactory hostFactory, DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(hostFactory);

        DAsyncFlow flow = RentFromCache(returnToCache: true);
        IDAsyncHost host = hostFactory.CreateHost(flow);
        return flow.ResumeCoreAsync(host, id, exception, cancellationToken);
    }
}
