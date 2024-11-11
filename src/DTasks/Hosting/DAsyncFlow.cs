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
    private Continuation? _continuation;

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
        _parentId = id;
        _cancellationToken = cancellationToken;
        Initialize(host);

        Resume();
        return new ValueTask(this, _valueTaskSource.Version);
    }

    public ValueTask ResumeAsync<TResult>(IDAsyncHost host, DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        Debug.Assert(_state is FlowState.Pending);

        _state = FlowState.Running;
        _parentId = id;
        _cancellationToken = cancellationToken;
        Initialize(host);

        Resume(result);
        return new ValueTask(this, _valueTaskSource.Version);
    }

    public ValueTask ResumeAsync(IDAsyncHost host, DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        Debug.Assert(_state is FlowState.Pending);

        _state = FlowState.Running;
        _parentId = id;
        _cancellationToken = cancellationToken;
        Initialize(host);

        Resume(exception);
        return new ValueTask(this, _valueTaskSource.Version);
    }

    private void Initialize(IDAsyncHost host)
    {
        _host = host;
        _marshaler = host.CreateMarshaler();
        _stateManager = host.CreateStateManager(this);
    }

    private void Resume()
    {
        if (_parentId.IsRoot)
        {
            Succeed();
        }
        else
        {
            Hydrate();
        }
    }

    private void Resume<TResult>(TResult result)
    {
        if (_parentId.IsRoot)
        {
            Succeed(result);
        }
        else
        {
            Hydrate(result);
        }
    }

    private void Resume(Exception exception)
    {
        if (_parentId.IsRoot)
        {
            Fail(exception);
        }
        else
        {
            Hydrate(exception);
        }
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

    private void Hydrate()
    {
        _state = FlowState.Hydrating;
        _id = _parentId;

        try
        {
            Await(_stateManager.HydrateAsync(_id, _cancellationToken));
        }
        catch (Exception ex)
        {
            _valueTaskSource.SetException(ex);
        }
    }

    private void Hydrate<TResult>(TResult result)
    {
        _state = FlowState.Hydrating;
        _id = _parentId;

        try
        {
            Await(_stateManager.HydrateAsync(_id, result, _cancellationToken));
        }
        catch (Exception ex)
        {
            _valueTaskSource.SetException(ex);
        }
    }

    private void Hydrate(Exception exception)
    {
        _state = FlowState.Hydrating;
        _id = _parentId;

        try
        {
            Await(_stateManager.HydrateAsync(_id, exception, _cancellationToken));
        }
        catch (Exception ex)
        {
            _valueTaskSource.SetException(ex);
        }
    }

    private void Dehydrate<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : notnull
    {
        DAsyncId parentId = _parentId;
        DAsyncId id = _id;

        _state = FlowState.Dehydrating;
        _parentId = _id;
        _id = DAsyncId.New();

        try
        {
            Await(_stateManager.DehydrateAsync(parentId, id, ref stateMachine, this, _cancellationToken));
        }
        catch (Exception ex)
        {
            _continuation = null;
            _suspendingAwaiterOrType = null;
            _valueTaskSource.SetException(ex);
        }
    }

    private void Reset()
    {
        Debug.Assert(_suspendingAwaiterOrType is null);
        Debug.Assert(_delay == default);
        Debug.Assert(_callback is null);
        Debug.Assert(_continuation is null);

        _state = FlowState.Pending;
        _valueTaskSource.Reset();
        _cancellationToken = default;

        _host = s_nullHost;
        _marshaler = s_nullMarshaler;
        _stateManager = s_nullStateManager;

        _parentId = default;
        _id = default;
    }

    [DebuggerStepThrough]
    private T Consume<T>([MaybeNull] ref T value)
    {
        T result = value;
        value = default;
        return result;
    }
}
