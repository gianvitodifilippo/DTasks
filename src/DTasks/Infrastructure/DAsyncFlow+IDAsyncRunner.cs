using System.Diagnostics;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncRunnerInternal
{
    IDAsyncCancellationManager IDAsyncRunner.Cancellation => throw new NotImplementedException();

    IDAsyncFeatureCollection IDAsyncRunner.Features => this;

    private void Suspend(DehydrateContinuation continuation)
    {
        Debug.Assert(_state is FlowState.Running);
        
        IDAsyncStateMachine stateMachine = ConsumeNotNull(ref _stateMachine);
        Assign(ref _dehydrateContinuation, continuation);
        stateMachine.Suspend();
    }

    private void Start()
    {
        IDAsyncStateMachine stateMachine = ConsumeNotNull(ref _childStateMachine);
        Assign(ref _stateMachine, stateMachine);
        if (_state is not FlowState.Hydrating)
        {
            _parentId = _id;
            _id = _idFactory.NewId();
        }
            
        _state = FlowState.Running;
        stateMachine.Start(this);
        stateMachine.MoveNext();
    }
    
    void IDAsyncRunner.Start(IDAsyncStateMachine stateMachine)
    {
        Assign(ref _childStateMachine, stateMachine);

        if (_state is not FlowState.Running)
        {
            Start();
            return;
        }
        
        Suspend(static self => self.Start());
    }

    void IDAsyncRunner.Succeed()
    {
        if (_state is FlowState.Hydrating)
        {
            _id = _parentId;
            _parentId = default;
        }
        
        if (_id.IsFlow)
        {
            AwaitOnSucceed();
            return;
        }

        if (_state is not FlowState.Running)
        {
            AwaitHydrate();
            return;
        }
        
        Suspend(static self =>
        {
            self._stateMachine = null;
            self.AwaitHydrate();
        });
    }

    void IDAsyncRunner.Succeed<TResult>(TResult result)
    {
        if (_state is FlowState.Hydrating)
        {
            _id = _parentId;
            _parentId = default;
        }

        if (_id.IsFlow)
        {
            AwaitOnSucceed(result);
            return;
        }

        if (_state is not FlowState.Running)
        {
            AwaitHydrate(result);
            return;
        }
        
        Suspend(self =>
        {
            self._stateMachine = null;
            self.AwaitHydrate(result);
        });
    }
    
    void IDAsyncRunner.Fail(Exception exception)
    {
        if (_state is FlowState.Hydrating)
        {
            _id = _parentId;
            _parentId = default;
        }

        if (_id.IsFlow)
        {
            AwaitOnFail(exception);
            return;
        }

        if (_state is not FlowState.Running)
        {
            AwaitHydrate(exception);
            return;
        }
        
        Suspend(self =>
        {
            self._stateMachine = null;
            self.AwaitHydrate(exception);
        });
    }
    
    void IDAsyncRunner.Cancel(OperationCanceledException exception)
    {
        if (_state is FlowState.Hydrating)
        {
            _id = _parentId;
            _parentId = default;
        }

        if (_id.IsFlow)
        {
            AwaitOnCancel(exception);
            return;
        }

        if (_state is not FlowState.Running)
        {
            AwaitHydrate(exception as Exception);
            return;
        }
        
        Suspend(self =>
        {
            self._stateMachine = null;
            self.AwaitHydrate(exception as Exception);
        });
    }

    void IDAsyncRunner.Yield()
    {
        if (_state is not FlowState.Running)
        {
            RunYieldIndirection();
            return;
        }
        
        Suspend(self => self.RunYieldIndirection());
    }

    void IDAsyncRunner.Delay(TimeSpan delay)
    {
        Assign(ref _delay, delay);
        
        if (_state is not FlowState.Running)
        {
            RunDelayIndirection();
            return;
        }
        
        Suspend(self => self.RunDelayIndirection());
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
