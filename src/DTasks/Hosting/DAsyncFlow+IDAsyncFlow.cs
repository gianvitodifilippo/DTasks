namespace DTasks.Hosting;

internal partial class DAsyncFlow : IDAsyncFlowInternal
{
    void IDAsyncFlow.Start(IDAsyncStateMachine stateMachine)
    {
        IDAsyncStateMachine? currentStateMachine = _stateMachine;
        _stateMachine = stateMachine;

        if (currentStateMachine is null)
        {
            stateMachine.Start(this);
            stateMachine.MoveNext();
        }
        else
        {
            _continuation = s_startContinuation;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.Succeed()
    {
        if (_parentId.IsRoot)
        {
            Succeed();
        }
        else
        {
            Hydrate(_parentId);
        }
    }

    void IDAsyncFlow.Succeed<TResult>(TResult result)
    {
        if (_parentId.IsRoot)
        {
            Succeed(result);
        }
        else
        {
            Hydrate(_parentId, result);
        }
    }

    void IDAsyncFlow.Fail(Exception exception)
    {
        if (_parentId.IsRoot)
        {
            Fail(exception);
        }
        else
        {
            Hydrate(_parentId, exception);
        }
    }

    void IDAsyncFlow.Yield()
    {
        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);

        if (currentStateMachine is null)
        {
            RunIndirection(s_yieldContinuation);
        }
        else
        {
            _continuation = s_yieldIndirectionContinuation;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.Delay(TimeSpan delay)
    {
        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);
        _delay = delay;

        if (currentStateMachine is null)
        {
            RunIndirection(s_delayContinuation);
        }
        else
        {
            _continuation = s_delayIndirectionContinuation;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlowInternal.Callback(ISuspensionCallback callback)
    {
        IDAsyncStateMachine? currentStateMachine = Consume(ref _stateMachine);
        _callback = callback;

        if (currentStateMachine is null)
        {
            RunIndirection(s_callbackContinuation);
        }
        else
        {
            _continuation = s_callbackIndirectionContinuation;
            currentStateMachine.Suspend();
        }
    }

    void IDAsyncFlow.WhenAll(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback callback)
    {
        throw new NotImplementedException();
    }

    void IDAsyncFlow.WhenAll<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultCallback<TResult[]> callback)
    {
        throw new NotImplementedException();
    }

    void IDAsyncFlow.WhenAny(IEnumerable<IDAsyncRunnable> tasks, IDAsyncResultCallback<DTask> callback)
    {
        throw new NotImplementedException();
    }

    void IDAsyncFlow.WhenAny<TResult>(IEnumerable<IDAsyncRunnable> tasks, IDAsyncResultCallback<DTask<TResult>> callback)
    {
        throw new NotImplementedException();
    }

    void IDAsyncFlow.Run(IDAsyncRunnable runnable, IDAsyncResultCallback<DTask> callback)
    {
        throw new NotImplementedException();
    }

    void IDAsyncFlow.Run<TResult>(IDAsyncRunnable runnable, IDAsyncResultCallback<DTask<TResult>> callback)
    {
        throw new NotImplementedException();
    }

    void IDAsyncFlowInternal.Handle(DAsyncId id)
    {
        throw new NotImplementedException();
    }

    void IDAsyncFlowInternal.Handle<TResult>(DAsyncId id)
    {
        throw new NotImplementedException();
    }
}
