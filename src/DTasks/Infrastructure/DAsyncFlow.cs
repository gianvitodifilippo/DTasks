using DTasks.Execution;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Utils;

namespace DTasks.Infrastructure;

public sealed partial class DAsyncFlow
{
    private FlowState _state;
    private AsyncTaskMethodBuilder _builder;
    private ManualResetValueTaskSourceCore<VoidDTaskResult> _valueTaskSource;
    private CancellationToken _cancellationToken;
    private IDAsyncHost _host;
    
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

    private readonly DTaskTokenConverter _taskTokenConverter;
    private readonly Dictionary<DTask, DTaskToken> _tokens;
    private readonly Dictionary<DAsyncId, DTask> _tasks;
    private readonly ConcurrentDictionary<DCancellationTokenSource, DistributedCancellationInfo> _cancellationInfos;
    private readonly ConcurrentDictionary<DCancellationId, DCancellationTokenSource> _cancellations;

    private DAsyncFlow()
    {
        _state = FlowState.Pending;
        _builder = AsyncTaskMethodBuilder.Create();
        _host = s_nullHost;
        _taskTokenConverter = new DTaskTokenConverter(this);
        _tokens = [];
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

        _state = FlowState.Running;
        _cancellationToken = cancellationToken;
        _parentId = DAsyncId.RootId;
        _id = DAsyncId.New();
        Initialize(host);

        runnable.Run(this);
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

    public ValueTask ResumeCoreAsync<TResult>(IDAsyncHost host, DAsyncId id, TResult result, CancellationToken cancellationToken = default)
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

    private void AwaitOnStart()
    {
        throw new NotImplementedException();
    }

    private void AwaitOnSuspend()
    {
        Await(_host.OnSuspendAsync(_cancellationToken), FlowState.Returning);
    }

    private void AwaitOnSucceed()
    {
        Await(_host.OnSucceedAsync(_cancellationToken), FlowState.Returning);
    }

    private void AwaitOnSucceed<TResult>(TResult result)
    {
        Await(_host.OnSucceedAsync(result, _cancellationToken), FlowState.Returning);
    }

    private void AwaitOnFail(Exception exception)
    {
        Await(_host.OnFailAsync(exception, _cancellationToken), FlowState.Returning);
    }

    private void AwaitOnCancel(OperationCanceledException exception)
    {
        Await(_host.OnCancelAsync(exception, _cancellationToken), FlowState.Returning);
    }

    private void Return()
    {
        Await(Task.CompletedTask, FlowState.Returning);
    }

    [DebuggerStepThrough]
    private static T Consume<T>([MaybeNull] ref T value)
    {
        T result = value;
        value = default;
        return result;
    }

    public static DAsyncFlow Create(ExecutionMode executionMode)
    {
        ThrowHelper.ThrowIfNull(executionMode);

        DAsyncFlow flow = RentFromCache();
        
#if DEBUG
        GC.ReRegisterForFinalize(flow);
#endif
        
        return flow;
    }
    
    public static ValueTask StartAsync(ExecutionMode executionMode, IDAsyncHost host, IDAsyncRunnable runnable,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(executionMode);
        ThrowHelper.ThrowIfNull(host);
        ThrowHelper.ThrowIfNull(runnable);
        
        DAsyncFlow flow = RentFromCache();
        flow._returnToCache = true;
        
        return flow.StartCoreAsync(host, runnable, cancellationToken);
    }
    
    public static ValueTask ResumeAsync(ExecutionMode executionMode, IDAsyncHost host, DAsyncId id,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(executionMode);
        ThrowHelper.ThrowIfNull(host);
        
        DAsyncFlow flow = RentFromCache();
        flow._returnToCache = true;
        
        return flow.ResumeCoreAsync(host, id, cancellationToken);
    }
    
    public static ValueTask ResumeAsync<TResult>(ExecutionMode executionMode, IDAsyncHost host, DAsyncId id, TResult result,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(executionMode);
        ThrowHelper.ThrowIfNull(host);
        
        DAsyncFlow flow = RentFromCache();
        flow._returnToCache = true;
        
        return flow.ResumeCoreAsync(host, id, result, cancellationToken);
    }
    
    public static ValueTask ResumeAsync(ExecutionMode executionMode, IDAsyncHost host, DAsyncId id, Exception exception,
        CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(executionMode);
        ThrowHelper.ThrowIfNull(host);
        
        DAsyncFlow flow = RentFromCache();
        flow._returnToCache = true;
        
        return flow.ResumeCoreAsync(host, id, exception, cancellationToken);
    }
}
