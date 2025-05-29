namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncRunnerInternal
{
    IDAsyncCancellationManager IDAsyncRunner.Cancellation => throw new NotImplementedException();

    IDAsyncFeatureCollection IDAsyncRunner.Features => this;
    
    void IDAsyncRunner.Start(IDAsyncStateMachine stateMachine)
    {
        AssertState<IDAsyncRunner>(FlowState.Running);

        Assign(ref _stateMachine, stateMachine);
        _parentId = _id;
        _id = DAsyncId.New();
        
        stateMachine.Start(this);
        stateMachine.MoveNext();
    }

    void IDAsyncRunner.Succeed()
    {
        AssertState<IDAsyncRunner>(FlowState.Running);

        if (_stateMachine is not null)
        {
            _stateMachine.Suspend();
            return;
        }
        
        if (_id.IsFlow)
        {
            AwaitOnSucceed();
            return;
        }

        ResumeParent();
    }

    void IDAsyncRunner.Succeed<TResult>(TResult result)
    {
        AssertState<IDAsyncRunner>(FlowState.Running);
        
        if (_id.IsFlow)
        {
            AwaitOnSucceed(result);
            return;
        }
        
        ResumeParent(result);
    }

    void IDAsyncRunner.Fail(Exception exception)
    {
        AssertState<IDAsyncRunner>(FlowState.Running);
        
        if (_id.IsFlow)
        {
            AwaitOnFail(exception);
            return;
        }
        
        ResumeParent(exception);
    }

    void IDAsyncRunner.Cancel(OperationCanceledException exception)
    {
        AssertState<IDAsyncRunner>(FlowState.Running);
        
        if (_id.IsFlow)
        {
            AwaitOnCancel(exception);
            return;
        }
        
        ResumeParent(exception);
    }

    void IDAsyncRunner.Yield()
    {
        AssertState<IDAsyncRunner>(FlowState.Running);
        
        AwaitRedirect(s_yieldIndirection, errorHandler: null);
    }

    void IDAsyncRunner.Delay(TimeSpan delay)
    {
        AssertState<IDAsyncRunner>(FlowState.Running);
        
        Assign(ref _delay, delay);
        AwaitRedirect(s_delayIndirection, ErrorHandlers.Indirection.Delay);
    }

    void IDAsyncRunner.WhenAll(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder builder)
    {
        AssertState<IDAsyncRunner>(FlowState.Running);
        
        throw new NotImplementedException();
    }

    void IDAsyncRunner.WhenAll<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder<TResult[]> builder)
    {
        AssertState<IDAsyncRunner>(FlowState.Running);
        
        throw new NotImplementedException();
    }

    void IDAsyncRunner.WhenAny(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder<DTask> builder)
    {
        AssertState<IDAsyncRunner>(FlowState.Running);
        
        throw new NotImplementedException();
    }

    void IDAsyncRunner.WhenAny<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder<DTask<TResult>> builder)
    {
        AssertState<IDAsyncRunner>(FlowState.Running);
        
        throw new NotImplementedException();
    }

    void IDAsyncRunner.Background(IDAsyncRunnable runnable, IDAsyncResultBuilder<DTask> builder)
    {
        AssertState<IDAsyncRunner>(FlowState.Running);
        
        throw new NotImplementedException();
    }

    void IDAsyncRunner.Background<TResult>(IDAsyncRunnable runnable, IDAsyncResultBuilder<DTask<TResult>> builder)
    {
        AssertState<IDAsyncRunner>(FlowState.Running);
        
        throw new NotImplementedException();
    }

    void IDAsyncRunner.Await(Task task, IDAsyncResultBuilder<Task> builder)
    {
        AssertState<IDAsyncRunner>(FlowState.Running);
        
        throw new NotImplementedException();
    }

    void IDAsyncRunnerInternal.Handle(DAsyncId id, IDAsyncResultBuilder builder)
    {
        AssertState<IDAsyncRunner>(FlowState.Running);
        
        throw new NotImplementedException();
    }

    void IDAsyncRunnerInternal.Handle<TResult>(DAsyncId id, IDAsyncResultBuilder<TResult> builder)
    {
        AssertState<IDAsyncRunner>(FlowState.Running);
        
        throw new NotImplementedException();
    }

    private void Run(IDAsyncRunnable runnable)
    {
        _state = FlowState.Running;
        runnable.Run(this);
        
        // IMPORTANT: Since runnable may invoke methods that await, it's crucial that calling this method
        // is the last thing that happens inside IAsyncStateMachine.MoveNext, otherwise we may run into
        // concurrency problems
    }
}
