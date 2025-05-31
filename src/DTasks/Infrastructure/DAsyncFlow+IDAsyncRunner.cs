namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncRunnerInternal
{
    IDAsyncCancellationManager IDAsyncRunner.Cancellation => throw new NotImplementedException();

    IDAsyncFeatureCollection IDAsyncRunner.Features => this;
    
    void IDAsyncRunner.Start(IDAsyncStateMachine stateMachine)
    {
        if (_state is not FlowState.Hydrating)
        {
            _parentId = _id;
            _id = DAsyncId.New();
        }

        _state = FlowState.Running;
        Assign(ref _stateMachine, stateMachine);
        
        stateMachine.Start(this);
        stateMachine.MoveNext();
    }

    void IDAsyncRunner.Succeed()
    {
        if (_state is FlowState.Hydrating)
        {
            _id = _parentId;
            _parentId = default;
        }
        
        if (_stateMachine is null)
        {
            AwaitOnSucceed();
            return;
        }
        
        Assign(ref _dehydrateContinuation, static flow => flow.AwaitHydrate());
        _stateMachine.Suspend();
    }

    void IDAsyncRunner.Succeed<TResult>(TResult result)
    {
        if (_state is FlowState.Hydrating)
        {
            _id = _parentId;
            _parentId = default;
        }

        if (_stateMachine is null)
        {
            AwaitOnSucceed(result);
            return;
        }
        
        Assign(ref _dehydrateContinuation, flow => flow.AwaitHydrate(result));
        _stateMachine.Suspend();
    }
    
    void IDAsyncRunner.Fail(Exception exception)
    {
        if (_state is FlowState.Hydrating)
        {
            _id = _parentId;
            _parentId = default;
        }

        if (_stateMachine is null)
        {
            AwaitOnFail(exception);
            return;
        }
        
        Assign(ref _dehydrateContinuation, flow => flow.AwaitHydrate(exception));
        _stateMachine.Suspend();
    }
    
    void IDAsyncRunner.Cancel(OperationCanceledException exception)
    {
        if (_state is FlowState.Hydrating)
        {
            _id = _parentId;
            _parentId = default;
        }

        if (_stateMachine is null)
        {
            AwaitOnCancel(exception);
            return;
        }
        
        Assign(ref _dehydrateContinuation, flow => flow.AwaitHydrate(exception as Exception));
        _stateMachine.Suspend();
    }

    void IDAsyncRunner.Yield()
    {
        RunIndirection(static flow => flow.AwaitOnYield());
    }

    void IDAsyncRunner.Delay(TimeSpan delay)
    {
        Assign(ref _delay, delay);
        RunIndirection(static flow => flow.AwaitOnDelay());
    }

    void IDAsyncRunner.WhenAll(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder builder)
    {
        throw new NotImplementedException();
    }

    void IDAsyncRunner.WhenAll<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder<TResult[]> builder)
    {
        throw new NotImplementedException();
    }

    void IDAsyncRunner.WhenAny(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder<DTask> builder)
    {
        throw new NotImplementedException();
    }

    void IDAsyncRunner.WhenAny<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder<DTask<TResult>> builder)
    {
        throw new NotImplementedException();
    }

    void IDAsyncRunner.Background(IDAsyncRunnable runnable, IDAsyncResultBuilder<DTask> builder)
    {
        throw new NotImplementedException();
    }

    void IDAsyncRunner.Background<TResult>(IDAsyncRunnable runnable, IDAsyncResultBuilder<DTask<TResult>> builder)
    {
        throw new NotImplementedException();
    }

    void IDAsyncRunner.Await(Task task, IDAsyncResultBuilder<Task> builder)
    {
        throw new NotImplementedException();
    }

    void IDAsyncRunnerInternal.Handle(DAsyncId id, IDAsyncResultBuilder builder)
    {
        throw new NotImplementedException();
    }

    void IDAsyncRunnerInternal.Handle<TResult>(DAsyncId id, IDAsyncResultBuilder<TResult> builder)
    {
        throw new NotImplementedException();
    }
}
