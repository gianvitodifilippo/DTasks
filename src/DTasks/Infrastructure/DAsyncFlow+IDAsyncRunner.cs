using System.Diagnostics;
using DTasks.Utils;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncRunnerInternal
{
    IDAsyncCancellationManager IDAsyncRunner.Cancellation
    {
        get
        {
            EnsureNotMarshaling();

            throw new NotImplementedException();
        }
    }

    IDAsyncFeatureCollection IDAsyncRunner.Features
    {
        get
        {
            EnsureNotMarshaling();

            return this;
        }
    }

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
        if (IsMarshaling)
        {
            Assign(ref _dehydrateContinuation, static self =>
            {
                Assert.NotNull(self._stateMachine);

                self._state = FlowState.Running;
                self._marshalingId = null;
                Assign(ref self._dehydrateContinuation, static self => self.AwaitHydrate());
                self._stateMachine.Suspend();
            });
            stateMachine.Start(this);
            stateMachine.Suspend();
            return;
        }
        
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
        EnsureNotMarshaling();
        
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
        EnsureNotMarshaling();

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
        EnsureNotMarshaling();

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
        EnsureNotMarshaling();

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
        EnsureNotMarshaling();

        if (_state is not FlowState.Running)
        {
            RunYieldIndirection();
            return;
        }
        
        Suspend(self => self.RunYieldIndirection());
    }

    void IDAsyncRunner.Delay(TimeSpan delay)
    {
        EnsureNotMarshaling();

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
        EnsureNotMarshaling();

        throw new NotImplementedException();
    }

    void IDAsyncRunner.WhenAll<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder<TResult[]> builder)
    {
        EnsureNotMarshaling();

        throw new NotImplementedException();
    }

    void IDAsyncRunner.WhenAny(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder<DTask> builder)
    {
        EnsureNotMarshaling();

        throw new NotImplementedException();
    }

    void IDAsyncRunner.WhenAny<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder<DTask<TResult>> builder)
    {
        EnsureNotMarshaling();

        throw new NotImplementedException();
    }

    void IDAsyncRunner.Background(IDAsyncRunnable runnable, IDAsyncResultBuilder<DTask> builder)
    {
        EnsureNotMarshaling();

        throw new NotImplementedException();
    }

    void IDAsyncRunner.Background<TResult>(IDAsyncRunnable runnable, IDAsyncResultBuilder<DTask<TResult>> builder)
    {
        EnsureNotMarshaling();

        throw new NotImplementedException();
    }

    void IDAsyncRunner.Await(Task task, IDAsyncResultBuilder<Task> builder)
    {
        EnsureNotMarshaling();

        throw new NotImplementedException();
    }

    void IDAsyncRunnerInternal.Handle(DAsyncId id, IDAsyncResultBuilder builder)
    {
        EnsureNotMarshaling();

        throw new NotImplementedException();
    }

    void IDAsyncRunnerInternal.Handle<TResult>(DAsyncId id, IDAsyncResultBuilder<TResult> builder)
    {
        EnsureNotMarshaling();

        throw new NotImplementedException();
    }
}
