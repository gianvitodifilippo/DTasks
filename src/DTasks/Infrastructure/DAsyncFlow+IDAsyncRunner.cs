using System.Diagnostics;
using System.Runtime.CompilerServices;
using DTasks.Utils;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncRunnerInternal
{
    IDAsyncCancellationManager IDAsyncRunner.Cancellation => throw new NotImplementedException();

    IDAsyncFeatureCollection IDAsyncRunner.Features => this;
    
    void IDAsyncRunner.Start(IDAsyncStateMachine stateMachine)
    {
        IDAsyncStateMachine? previousStateMachine = Consume(ref _stateMachine);
        if (previousStateMachine is not null)
        {
            _stateMachine = stateMachine;
            Assign(ref _dehydrateContinuation, static self =>
            {
                IDAsyncStateMachine? stateMachine = self._stateMachine;
                Assert.NotNull(stateMachine);
                
                self._state = FlowState.Running;
                self._parentId = self._id;
                self._id = self._idFactory.NewId();
                stateMachine.Start(self);
                stateMachine.MoveNext();
            });
            previousStateMachine.Suspend();
            return;
        }
        
        if (_state is not FlowState.Hydrating)
        {
            _parentId = _id;
            _id = _idFactory.NewId();
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
        
        if (_id.IsFlow)
        {
            AwaitOnSucceed();
            return;
        }

        if (_stateMachine is null)
        {
            AwaitHydrate();
            return;
        }
        
        Assign(ref _dehydrateContinuation, static self =>
        {
            self._stateMachine = null;
            self.AwaitHydrate();
        });
        _stateMachine.Suspend();
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

        if (_stateMachine is null)
        {
            AwaitHydrate(result);
            return;
        }
        
        Assign(ref _dehydrateContinuation, self =>
        {
            self._stateMachine = null;
            self.AwaitHydrate(result);
        });
        _stateMachine.Suspend();
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

        if (_stateMachine is null)
        {
            AwaitHydrate();
            return;
        }
        
        Assign(ref _dehydrateContinuation, self =>
        {
            self._stateMachine = null;
            self.AwaitHydrate(exception);
        });
        _stateMachine.Suspend();
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

        if (_stateMachine is null)
        {
            AwaitHydrate();
            return;
        }
        
        Assign(ref _dehydrateContinuation, self =>
        {
            self._stateMachine = null;
            self.AwaitHydrate(exception as Exception);
        });
        _stateMachine.Suspend();
    }

    void IDAsyncRunner.Yield()
    {
        IDAsyncStateMachine? stateMachine = Consume(ref _stateMachine);
        if (stateMachine is not null)
        {
            Assign(ref _dehydrateContinuation, static self =>
            {
                self.RunIndirection(static self => self.AwaitOnYield());
            });
            stateMachine.Suspend();
            return;
        }
        
        RunIndirection(static self => self.AwaitOnYield());
    }

    void IDAsyncRunner.Delay(TimeSpan delay)
    {
        IDAsyncStateMachine? stateMachine = Consume(ref _stateMachine);
        if (stateMachine is not null)
        {
            Assign(ref _delay, delay);
            Assign(ref _dehydrateContinuation, static self =>
            {
                self.RunIndirection(static self => self.AwaitOnDelay());
            });
            stateMachine.Suspend();
            return;
        }
        
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
