using System.Diagnostics;
using DTasks.Utils;

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
        StartFrame();
            
        _state = FlowState.Running;
        stateMachine.Start(this);
        stateMachine.MoveNext();
    }
    
    void IDAsyncRunner.Start(IDAsyncStateMachine stateMachine)
    {
        if (_node is not null)
        {
            Assign(ref _dehydrateContinuation, static flow => flow.RunYieldIndirection());
            StartFrame();
            stateMachine.Start(this);
            stateMachine.Suspend();
            return;
        }
        
        Assign(ref _childStateMachine, stateMachine);

        if (_stateMachine is not null)
        {
            Suspend(static self => self.Start());
            return;
        }
        
        Start();
    }

    void IDAsyncRunner.Succeed()
    {
        if (_node is not null)
        {
            _node.SucceedBranch();
            return;
        }
        
        if (_frameHasIds)
        {
            _frameHasIds = false;

            if (_parentId.IsDefault)
            {
                Assign(ref _dehydrateContinuation, static self => self.AwaitFlush());
                AwaitDehydrateCompleted();
                return;
            }

            _childId = _id;
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
        
        Suspend(static self =>
        {
            self._stateMachine = null;
            self.AwaitHydrate();
        });
    }

    void IDAsyncRunner.Succeed<TResult>(TResult result)
    {
        if (_node is not null)
        {
            _node.SucceedBranch(result);
            return;
        }

        if (_frameHasIds)
        {
            _frameHasIds = false;
            
            if (_parentId.IsDefault)
            {
                Assign(ref _dehydrateContinuation, static self => self.AwaitFlush());
                AwaitDehydrateCompleted(result);
                return;
            }

            _childId = _id;
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
        
        Suspend(self =>
        {
            self._stateMachine = null;
            self.AwaitHydrate(result);
        });
    }
    
    void IDAsyncRunner.Fail(Exception exception)
    {
        if (_node is not null)
        {
            _node.FailBranch(exception);
            return;
        }

        if (_frameHasIds)
        {
            _frameHasIds = false;

            if (_parentId.IsDefault)
            {
                Assign(ref _dehydrateContinuation, static self => self.AwaitFlush());
                AwaitDehydrateCompleted(exception);
                return;
            }

            _childId = _id;
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
        if (_node is not null)
        {
            _node.FailBranch(exception);
            return;
        }

        if (_frameHasIds)
        {
            _frameHasIds = false;

            if (_parentId.IsDefault)
            {
                Assign(ref _dehydrateContinuation, static self => self.AwaitFlush());
                AwaitDehydrateCompleted(exception as Exception);
                return;
            }

            _childId = _id;
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
        if (_stateMachine is not null)
        {
            Suspend(self => self.RunYieldIndirection());
            return;
        }
        
        RunYieldIndirection();
    }

    void IDAsyncRunner.Delay(TimeSpan delay)
    {
        Assign(ref _delay, delay);
        
        if (_stateMachine is not null)
        {
            Suspend(self => self.RunDelayIndirection());
            return;
        }
        
        RunDelayIndirection();
    }

    void IDAsyncRunner.WhenAll(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder builder)
    {
        IEnumerator<IDAsyncRunnable> branchEnumerator = runnables.GetEnumerator();
        if (!branchEnumerator.MoveNext())
        {
            branchEnumerator.Dispose();
            builder.SetResult();
            if (_stateMachine is null)
            {
                ((IDAsyncRunner)this).Succeed();
            }
            else
            {
                _suspendingAwaiterOrType = null;
                Continue();
            }
            return;
        }

        PushNode(new WhenAllFlowNode(this, branchEnumerator, builder));
        RunBranch();
    }

    void IDAsyncRunner.WhenAll<TResult>(IEnumerable<IDAsyncRunnable> runnables, IDAsyncResultBuilder<TResult[]> builder)
    {
        IEnumerator<IDAsyncRunnable> branchEnumerator = runnables.GetEnumerator();
        if (!branchEnumerator.MoveNext())
        {
            branchEnumerator.Dispose();
            builder.SetResult([]);
            if (_stateMachine is null)
            {
                ((IDAsyncRunner)this).Succeed(Array.Empty<TResult>());
            }
            else
            {
                _suspendingAwaiterOrType = null;
                Continue();
            }
            return;
        }

        PushNode(new WhenAllFlowNode<TResult>(this, branchEnumerator, builder));
        RunBranch();
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
        _handleId = id;
        Assign(ref _handleResultBuilder, builder);
        Assign(ref _handleBuilder, HandleBuilder.Instance);
        AwaitLink();
    }

    void IDAsyncRunnerInternal.Handle<TResult>(DAsyncId id, IDAsyncResultBuilder<TResult> builder)
    {
        _handleId = id;
        Assign(ref _handleResultBuilder, builder);
        Assign(ref _handleBuilder, HandleBuilder<TResult>.Instance);
        AwaitLink();
    }
}
