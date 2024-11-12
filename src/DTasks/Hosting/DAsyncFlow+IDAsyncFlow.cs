using DTasks.Utils;

namespace DTasks.Hosting;

internal partial class DAsyncFlow : IDAsyncFlowInternal
{
    void IDAsyncFlow.Start(IDAsyncStateMachine stateMachine)
    {
        ThrowHelper.ThrowIfNull(stateMachine);

        IDAsyncStateMachine? currentStateMachine = _stateMachine;
        _stateMachine = stateMachine;

        if (currentStateMachine is null)
        {
            Start(stateMachine);
        }
        else
        {
            _continuation = StartContinuation;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.Resume()
    {
        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            Resume(_parentId);
        }
        else
        {
            _continuation = self => self.Resume(self._parentId);
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.Resume<TResult>(TResult result)
    {
        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            Resume(_parentId, result);
        }
        else
        {
            _continuation = self => self.Resume(self._parentId, result);
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.Resume(Exception exception)
    {
        ThrowHelper.ThrowIfNull(exception);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            Resume(_parentId, exception);
        }
        else
        {
            _continuation = self => self.Resume(self._parentId, exception);
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.Yield()
    {
        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            RunIndirection(YieldContinuation);
        }
        else
        {
            _continuation = YieldIndirectionContinuation;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.Delay(TimeSpan delay)
    {
        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);
        _delay = delay;

        if (currentStateMachine is null)
        {
            RunIndirection(DelayContinuation);
        }
        else
        {
            _continuation = DelayIndirectionContinuation;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlowInternal.Callback(ISuspensionCallback callback)
    {
        ThrowHelper.ThrowIfNull(callback);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);
        _callback = callback;

        if (currentStateMachine is null)
        {
            RunIndirection(CallbackContinuation);
        }
        else
        {
            _continuation = CallbackIndirectionContinuation;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.WhenAll(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback callback)
    {
        ThrowHelper.ThrowIfNull(runnables);
        ThrowHelper.ThrowIfNull(callback);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            WhenAll(runnables, callback);
        }
        else
        {
            _aggregateBranches = runnables;
            _resultCallback = callback;
            _continuation = WhenAllContinuation;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.WhenAll<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback<TResult[]> callback)
    {
        ThrowHelper.ThrowIfNull(runnables);
        ThrowHelper.ThrowIfNull(callback);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            WhenAll(runnables, callback);
        }
        else
        {
            _aggregateBranches = runnables;
            _resultCallback = callback;
            _continuation = WhenAllContinuation<TResult>;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.WhenAny(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback<DTask> callback)
    {
        ThrowHelper.ThrowIfNull(runnables);
        ThrowHelper.ThrowIfNull(callback);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            WhenAny(runnables, callback);
        }
        else
        {
            _aggregateBranches = runnables;
            _resultCallback = callback;
            _continuation = WhenAnyContinuation;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.WhenAny<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback<DTask<TResult>> callback)
    {
        ThrowHelper.ThrowIfNull(runnables);
        ThrowHelper.ThrowIfNull(callback);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            WhenAny(runnables, callback);
        }
        else
        {
            _aggregateBranches = runnables;
            _resultCallback = callback;
            _continuation = WhenAnyContinuation<TResult>;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.Run(IDAsyncRunnable runnable, IDAsyncResultCallback<DTask> callback)
    {
        ThrowHelper.ThrowIfNull(runnable);
        ThrowHelper.ThrowIfNull(callback);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            Run(runnable, callback);
        }
        else
        {
            _backgroundRunnable = runnable;
            _resultCallback = callback;
            _continuation = RunContinuation;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.Run<TResult>(IDAsyncRunnable runnable, IDAsyncResultCallback<DTask<TResult>> callback)
    {
        ThrowHelper.ThrowIfNull(runnable);
        ThrowHelper.ThrowIfNull(callback);

        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            Run(runnable, callback);
        }
        else
        {
            _backgroundRunnable = runnable;
            _resultCallback = callback;
            _continuation = RunContinuation<TResult>;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlowInternal.Handle(DAsyncId id)
    {
        throw new NotImplementedException();
    }

    void IDAsyncFlowInternal.Handle<TResult>(DAsyncId id)
    {
        throw new NotImplementedException();
    }

    private void Start(IDAsyncStateMachine stateMachine)
    {
        stateMachine.Start(this);
        stateMachine.MoveNext();
    }

    private void Resume(DAsyncId id)
    {
        _id = id;

        if (id.IsRoot)
        {
            Succeed();
        }
        else
        {
            Hydrate(id);
        }
    }

    private void Resume<TResult>(DAsyncId id, TResult result)
    {
        _id = id;

        if (id.IsRoot)
        {
            Succeed(result);
        }
        else
        {
            Hydrate(id, result);
        }
    }

    private void Resume(DAsyncId id, Exception exception)
    {
        _id = id;

        if (id.IsRoot)
        {
            Fail(exception);
        }
        else
        {
            Hydrate(id, exception);
        }
    }

    private void Yield()
    {
        _suspendingAwaiterOrType = null;
        _state = FlowState.Returning;

        try
        {
            Await(_host.YieldAsync(_id, _cancellationToken));
        }
        catch (Exception ex)
        {
            _valueTaskSource.SetException(ex);
        }
    }

    private void Delay(TimeSpan delay)
    {
        _suspendingAwaiterOrType = null;
        _state = FlowState.Returning;

        try
        {
            Await(_host.DelayAsync(_id, delay, _cancellationToken));
        }
        catch (Exception ex)
        {
            _valueTaskSource.SetException(ex);
        }
    }

    private void Callback(ISuspensionCallback callback)
    {
        _suspendingAwaiterOrType = null;
        _state = FlowState.Returning;

        try
        {
            Await(callback.InvokeAsync(_id, _cancellationToken));
        }
        catch (Exception ex)
        {
            _valueTaskSource.SetException(ex);
        }
    }

    private void WhenAll(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback callback)
    {
        throw new NotImplementedException();
    }

    private void WhenAll<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback<TResult[]> callback)
    {
        throw new NotImplementedException();
    }

    private void WhenAny(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback<DTask> callback)
    {
        throw new NotImplementedException();
    }

    private void WhenAny<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback<DTask<TResult>> callback)
    {
        throw new NotImplementedException();
    }

    private void Run(IDAsyncRunnable runnable, IDAsyncResultCallback<DTask> callback)
    {
        throw new NotImplementedException();
    }

    private void Run<TResult>(IDAsyncRunnable runnable, IDAsyncResultCallback<DTask<TResult>> callback)
    {
        throw new NotImplementedException();
    }
}
