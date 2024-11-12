using DTasks.Marshaling;
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

    private TaskAwaiter _voidTa;
    private ValueTaskAwaiter _voidVta;
    private ValueTaskAwaiter<DAsyncLink> _linkVta;

    private DAsyncId _parentId;
    private DAsyncId _id;
    private IDAsyncStateMachine? _stateMachine;
    private object? _suspendingAwaiterOrType;
    private TimeSpan _delay;
    private ISuspensionCallback? _callback;
    private IEnumerable<IDAsyncRunnable>? _aggregateBranches;
    private IDAsyncRunnable? _backgroundRunnable;
    private object? _resultCallback;
    private FlowContinuation? _continuation;

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
    }

    private void Succeed()
    {
        _state = FlowState.Returning;

        try
        {
            Await(_host.SucceedAsync(_cancellationToken));
        }
        catch (Exception ex)
        {
            _valueTaskSource.SetException(ex);
        }
    }

    private void Succeed<TResult>(TResult result)
    {
        _state = FlowState.Returning;

        try
        {
            Await(_host.SucceedAsync(result, _cancellationToken));
        }
        catch (Exception ex)
        {
            _valueTaskSource.SetException(ex);
        }
    }

    private void Fail(Exception exception)
    {
        _state = FlowState.Returning;

        try
        {
            Await(_host.FailAsync(exception, _cancellationToken));
        }
        catch (Exception ex)
        {
            _valueTaskSource.SetException(ex);
        }
    }

    [DebuggerStepThrough]
    private T Consume<T>([MaybeNull] ref T value)
    {
        T result = value;
        value = default;
        return result;
    }
}
