using DTasks.Marshaling;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;

namespace DTasks.Hosting;

internal partial class DAsyncFlow
{
    private FlowState _state;
    private AsyncTaskMethodBuilder _builder;
    private ManualResetValueTaskSourceCore<VoidDTaskResult> _valueTaskSource;
    private CancellationToken _cancellationToken;

    private IDAsyncHost _host = s_nullHost;
    private IDAsyncMarshaler _marshaler = s_nullMarshaler;
    private IDAsyncStateManager _stateManager = s_nullStateManager;
    private ITypeResolver _typeResolver = s_nullTypeResolver;

    private TaskAwaiter _voidTa;
    private ValueTaskAwaiter _voidVta;
    private ValueTaskAwaiter<DAsyncLink> _linkVta;

    private DAsyncId _parentId;
    private DAsyncId _id;
    private IDAsyncStateMachine? _stateMachine;
    private object? _suspendingAwaiterOrType;
    private TimeSpan? _delay;
    private ISuspensionCallback? _callback;
    private AggregateType _aggregateType;
    private IEnumerable<IDAsyncRunnable>? _aggregateBranches;
    private List<Exception>? _aggregateExceptions;
    private int _whenAllBranchCount;
    private IDictionary? _whenAllBranchResults;
    private IDAsyncRunnable? _aggregateRunnable;
    private object? _resultBuilder;
    private Type? _handleResultType;
    private FlowContinuation? _continuation;

    private DAsyncFlow? _parent;
    private int _branchIndex = -1;

    private readonly Dictionary<DTask, DTaskToken> _tokens = [];
    private readonly Dictionary<DAsyncId, DTask> _tasks = [];

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
        Debug.Assert(_state is FlowState.Pending);

        _state = FlowState.Running;
        _cancellationToken = cancellationToken;
        _parentId = DAsyncId.RootId;
        _id = DAsyncId.New();
        Initialize(host);

        runnable.Run(this);
        return new ValueTask(this, _valueTaskSource.Version);
    }

    public ValueTask ResumeAsync(IDAsyncHost host, DAsyncId id, CancellationToken cancellationToken = default)
    {
        Debug.Assert(_state is FlowState.Pending);

        _state = FlowState.Running;
        _cancellationToken = cancellationToken;
        Initialize(host);

        Resume(id);
        return new ValueTask(this, _valueTaskSource.Version);
    }

    public ValueTask ResumeAsync<TResult>(IDAsyncHost host, DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        Debug.Assert(_state is FlowState.Pending);

        _state = FlowState.Running;
        _cancellationToken = cancellationToken;
        Initialize(host);

        Resume(id, result);
        return new ValueTask(this, _valueTaskSource.Version);
    }

    public ValueTask ResumeAsync(IDAsyncHost host, DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        Debug.Assert(_state is FlowState.Pending);

        _state = FlowState.Running;
        _cancellationToken = cancellationToken;
        Initialize(host);

        Resume(id, exception);
        return new ValueTask(this, _valueTaskSource.Version);
    }

    private void Initialize(IDAsyncHost host)
    {
        _host = host;
        _marshaler = host.CreateMarshaler();
        _stateManager = host.CreateStateManager(this);
        _typeResolver = host.TypeResolver;
    }

    private void Succeed()
    {
        Await(_host.SucceedAsync(_cancellationToken), FlowState.Returning);
    }

    private void Succeed<TResult>(TResult result)
    {
        Await(_host.SucceedAsync(result, _cancellationToken), FlowState.Returning);
    }

    private void Fail(Exception exception)
    {
        Await(_host.FailAsync(exception, _cancellationToken), FlowState.Returning);
    }

    [DebuggerStepThrough]
    private T Consume<T>([MaybeNull] ref T value)
    {
        T result = value;
        value = default;
        return result;
    }
}
