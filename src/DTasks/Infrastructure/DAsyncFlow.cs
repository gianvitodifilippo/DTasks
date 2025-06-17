using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;
using DTasks.Execution;
using DTasks.Infrastructure.DependencyInjection;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using DTasks.Utils;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : DAsyncRunner
{
    private readonly IDAsyncFlowPool _pool;
    private readonly IDAsyncInfrastructure _infrastructure;
    private readonly DAsyncIdFactory _idFactory;
    private readonly HostComponentProvider _hostComponentProvider;
    private readonly FlowComponentProvider _flowComponentProvider;
    private readonly Dictionary<object, object?> _flowProperties;
    
    private FlowState _state;
    private AsyncTaskMethodBuilder _builder;
    private ManualResetValueTaskSourceCore<VoidDTaskResult> _valueTaskSource;
    private CancellationToken _cancellationToken;

    private bool _hasError;
    private ErrorMessageProvider? _errorMessageProvider;
    private IDAsyncRunnable? _runnable;
    private object? _resultOrException;
    private DAsyncId _id;
    private DAsyncId _parentId;
    private DAsyncId _childId;
    private bool _frameHasIds;
    private IDAsyncStateMachine? _stateMachine;
    private object? _suspendingAwaiterOrType;
    private IDAsyncStateMachine? _childStateMachine;
    private TimeSpan? _delay;
    private ISuspensionCallback? _suspensionCallback;
    private DehydrateContinuation? _dehydrateContinuation;
    private object? _handleResultBuilder;
    private IHandleBuilder? _handleBuilder;
    private DAsyncId _handleId;

    private IFlowNode? _node;
    
    private TaskAwaiter _voidTa;
    private ValueTaskAwaiter _voidVta;
    private ValueTaskAwaiter<DAsyncLink> _linkVta;

    private IDAsyncHost _host;
    private IDAsyncStack? _stack;
    private IDAsyncHeap? _heap;
    private IDAsyncSurrogator? _surrogator;
    private IDAsyncCancellationProvider? _cancellationProvider;
    private IDAsyncSuspensionHandler? _suspensionHandler;
    
    private Dictionary<DTask, DAsyncId>? _handleIds;
    private Dictionary<DAsyncId, DTask>? _completedTasks;

#if DEBUG
    private string? _stackTrace;
#endif

    public DAsyncFlow(IDAsyncFlowPool pool, IDAsyncInfrastructure infrastructure, DAsyncIdFactory idFactory)
    {
        _pool = pool;
        _infrastructure = infrastructure;
        _idFactory = idFactory;
        _hostComponentProvider = infrastructure.RootProvider.CreateHostProvider(this);
        _flowComponentProvider = _hostComponentProvider.CreateFlowProvider(this);
        _flowProperties = [];
        
        _state = FlowState.Idling;
        _builder = AsyncTaskMethodBuilder.Create();
        _host = s_nullHost;
    }

    protected override IDAsyncHostInfrastructure InfrastructureCore => _hostComponentProvider;

    private IDAsyncTypeResolver TypeResolver => _infrastructure.TypeResolver;

    private IDAsyncStack Stack => _stack ??= _infrastructure.GetStack(_flowComponentProvider);

    private IDAsyncHeap Heap => _heap ??= _infrastructure.GetHeap(_flowComponentProvider);

    private IDAsyncSurrogator Surrogator => _surrogator ??= _infrastructure.GetSurrogator(_flowComponentProvider);

    private IDAsyncCancellationProvider CancellationProvider => _cancellationProvider ??= _infrastructure.GetCancellationProvider(_flowComponentProvider);

    private IDAsyncSuspensionHandler SuspensionHandler => _suspensionHandler ??= _infrastructure.GetSuspensionHandler(_flowComponentProvider);

    private Dictionary<DTask, DAsyncId> HandleIds => _handleIds ??= [];

    private Dictionary<DAsyncId, DTask> CompletedTasks => _completedTasks ??= [];

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
        ValidateState();

        _id = _idFactory.NewFlowId();
        Assign(ref _runnable, runnable);
        InitializeFlow(cancellationToken);
        AwaitOnStart();
        
        return new ValueTask(this, _valueTaskSource.Version);
    }

    protected override ValueTask ResumeCoreAsync(DAsyncId id, CancellationToken cancellationToken)
    {
        Validate(id);
        ValidateState();

        _id = id;
        InitializeFlow(cancellationToken);
        AwaitHydrate();
        
        return new ValueTask(this, _valueTaskSource.Version);
    }

    protected override ValueTask ResumeCoreAsync<TResult>(DAsyncId id, TResult result, CancellationToken cancellationToken)
    {
        Validate(id);
        ValidateState();

        _id = id;
        InitializeFlow(cancellationToken);
        AwaitHydrate(result);

        return new ValueTask(this, _valueTaskSource.Version);
    }

    protected override ValueTask ResumeCoreAsync(DAsyncId id, Exception exception, CancellationToken cancellationToken)
    {
        Validate(id);
        ValidateState();

        _id = id;
        InitializeFlow(cancellationToken);
        AwaitHydrate(exception);
        
        return new ValueTask(this, _valueTaskSource.Version);
    }

    private void InitializeFlow(CancellationToken cancellationToken)
    {
        // _clearFlowProperties = true;
        _flowComponentProvider.BeginScope();
        _cancellationToken = cancellationToken;
        _host.OnInitialize(this);
        // CancellationProvider.RegisterHandler(this);
    }

    private void ValidateState()
    {
        if (_state is not FlowState.Pending)
            throw new InvalidOperationException($"Detected invalid usage of {nameof(DAsyncFlow)}.");
    }

    private static void Validate(DAsyncId id)
    {
        if (id.IsFlow)
            throw new ArgumentException("The provided id cannot be used to resume a d-async flow, as it represents its root.");
    }

    private void StartFrame()
    {
        if (_frameHasIds)
        {
            _frameHasIds = false;
            return;
        }
        
        _parentId = _id;
        _id = _idFactory.NewId();

        if (_node is not null && _parentId == _node.NodeId)
        {
            _node.SetChildId(_id);
        }
    }

    [DebuggerStepThrough]
    private static T? Consume<T>(ref T? field)
    {
        T? result = field;
        field = default;
        return result;
    }

    [DebuggerStepThrough]
    private static T ConsumeNotNull<T>(ref T? field)
        where T : class
    {
        Debug.Assert(field is not null);
        
        T result = field;
        field = null;
        return result;
    }

    [DebuggerStepThrough]
    private static T ConsumeNotNull<T>(ref T? field)
        where T : struct
    {
        Debug.Assert(field is not null);
        
        T? result = field;
        field = null;
        return result.Value;
    }

    [DebuggerStepThrough]
    private static void Assign<T>([NotNullIfNotNull(nameof(value))] ref T? field, T? value)
        where T : class
    {
        Assert.Null(field);

        field = value;
    }

    [DebuggerStepThrough]
    private static void AssignStruct<T>(ref T field, T value)
        where T : struct, IEquatable<T>
    {
        Assert.Default(field);

        field = value;
    }

    [DebuggerStepThrough]
    private static void Assign<T>([NotNull] ref T? field, T value)
        where T : struct
    {
        Assert.Null(field);

        field = value;
    }

    [Conditional("DEBUG")]
    private void AssertState<TInterface>(FlowState state)
    {
        Debug.Assert(_state == state, $"{typeof(TInterface).Name} should be exposed only when the state is '{state}'. It was '{_state}'.");
    }
    
    private delegate void DehydrateContinuation(DAsyncFlow flow);
}
