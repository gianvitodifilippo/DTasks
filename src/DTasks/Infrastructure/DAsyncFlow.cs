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

    private IDAsyncHost _host;
    private IDAsyncStack? _stack;
    private IDAsyncHeap? _heap;
    private IDAsyncSurrogator? _surrogator;
    private IDAsyncCancellationProvider? _cancellationProvider;
    private IDAsyncSuspensionHandler? _suspensionHandler;

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

    private Dictionary<object, object?> _properties;
    private Dictionary<object, object> _components;
    private Dictionary<object, object> _scopedComponents;
    private bool _isCreatingComponent;
    private bool _usedPropertyInScopedComponent;

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
        _properties = [];
        _components = [];
        _scopedComponents = [];
    }

    private DTasksConfiguration Configuration => _host.Configuration;

    private IDAsyncTypeResolver TypeResolver => Configuration.TypeResolver;

    private IDAsyncStack Stack => GetComponent(ref _stack, static (infrastructure, scope) => infrastructure.GetStack(scope));

    private IDAsyncHeap Heap => GetComponent(ref _heap, static (infrastructure, scope) => infrastructure.GetHeap(scope));

    private IDAsyncSurrogator Surrogator => GetComponent(ref _surrogator, static (infrastructure, scope) => infrastructure.GetSurrogator(scope));

    private IDAsyncCancellationProvider CancellationProvider => GetComponent(ref _cancellationProvider, static (infrastructure, scope) => infrastructure.GetCancellationProvider(scope));

    private IDAsyncSuspensionHandler SuspensionHandler => GetComponent(ref _suspensionHandler, static (infrastructure, scope) => infrastructure.GetSuspensionHandler(scope));

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

    private void Initialize(IDAsyncHost host)
    {
        _host = host;
        
        host.OnInitialize(this);
        CancellationProvider.RegisterHandler(this);
    }

    protected override ValueTask StartCoreAsync(IDAsyncHost host, IDAsyncRunnable runnable, CancellationToken cancellationToken)
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

    protected override ValueTask ResumeCoreAsync(IDAsyncHost host, DAsyncId id, CancellationToken cancellationToken)
    {
        if (_state is not FlowState.Pending)
            throw new InvalidOperationException($"Detected invalid usage of {nameof(DAsyncFlow)}");

        _state = FlowState.Running;
        _cancellationToken = cancellationToken;

        Initialize(host);
        Resume(id);
        
        return new ValueTask(this, _valueTaskSource.Version);
    }

    protected override ValueTask ResumeCoreAsync<TResult>(IDAsyncHost host, DAsyncId id, TResult result, CancellationToken cancellationToken)
    {
        if (_state is not FlowState.Pending)
            throw new InvalidOperationException($"Detected invalid usage of {nameof(DAsyncFlow)}");

        _state = FlowState.Running;
        _cancellationToken = cancellationToken;
        
        Initialize(host);
        Resume(id, result);

        return new ValueTask(this, _valueTaskSource.Version);
    }

    protected override ValueTask ResumeCoreAsync(IDAsyncHost host, DAsyncId id, Exception exception, CancellationToken cancellationToken)
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

    private TComponent GetComponent<TComponent>([NotNull] ref TComponent? component, Func<IDAsyncInfrastructure, IDAsyncScope, TComponent> factory)
        where TComponent : notnull
    {
        if (component is not null)
            return component;

        _isCreatingComponent = true;
        component = factory(Configuration.Infrastructure, this);
        _isCreatingComponent = false;

        return component;
    }

#if DEBUG
    public static DAsyncFlow Create(string stackTrace)
    {
        DAsyncFlow flow = RentFromCache(returnToCache: false);

        GC.ReRegisterForFinalize(flow);
        flow._stackTrace = stackTrace;

        return flow;
    }
#else
    public static DAsyncFlow Create() => RentFromCache(returnToCache: false);
#endif
}
