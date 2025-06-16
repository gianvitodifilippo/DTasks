using DTasks.Marshaling;
using DTasks.Utils;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncMethodBuilder
{
    void IDAsyncMethodBuilder.AwaitOnCompleted<TAwaiter>(ref TAwaiter awaiter)
    {
        AssertState<IDAsyncMethodBuilder>(FlowState.Running);
        
        if (awaiter is IDAsyncAwaiter)
        {
            AwaitContinue(ref awaiter);
        }
        else
        {
            var self = this;
            _builder.AwaitOnCompleted(ref awaiter, ref self);
        }
    }

    void IDAsyncMethodBuilder.AwaitUnsafeOnCompleted<TAwaiter>(ref TAwaiter awaiter)
    {
        AssertState<IDAsyncMethodBuilder>(FlowState.Running);

        if (awaiter is IDAsyncAwaiter)
        {
            AwaitContinue(ref awaiter);
        }
        else
        {
            var self = this;
            _builder.AwaitUnsafeOnCompleted(ref awaiter, ref self);
        }
    }

    private void AwaitContinue<TAwaiter>(ref TAwaiter awaiter)
    {
        Assert.Is<IDAsyncAwaiter>(awaiter);
        
        if (((IDAsyncAwaiter)awaiter).IsCompleted)
        {
            Continue();
        }
        else
        {
            Assign(ref _suspendingAwaiterOrType, typeof(TAwaiter).IsValueType
                ? typeof(TAwaiter)
                : awaiter);

            try
            {
                ((IDAsyncAwaiter)awaiter).Continue(this);
            }
            catch (Exception ex) when (ex is not MarshalingException)
            {
                _suspendingAwaiterOrType = null;
                HandleException(ex);
            }
        }
    }

    void IDAsyncMethodBuilder.SetResult()
    {
        AssertState<IDAsyncMethodBuilder>(FlowState.Running);

        if (_stateMachine is DTask task)
        {
            CompletedTasks.Add(_id, task);
        }

        _stateMachine = null;
        
        if (_parentId.IsDefault)
        {
            Assign(ref _dehydrateContinuation, static self => self.AwaitFlush());
            AwaitDehydrateCompleted();
            return;
        }
        
        _id = _parentId;
        _parentId = default;
        
        if (_id.IsFlow)
        {
            AwaitOnSucceed();
            return;
        }
        
        AwaitHydrate();
    }

    void IDAsyncMethodBuilder.SetResult<TResult>(TResult result)
    {
        AssertState<IDAsyncMethodBuilder>(FlowState.Running);

        if (_stateMachine is DTask task)
        {
            CompletedTasks.Add(_id, task);
        }

        _stateMachine = null;
        
        if (_parentId.IsDefault)
        {
            Assign(ref _dehydrateContinuation, static self => self.AwaitFlush());
            AwaitDehydrateCompleted(result);
            return;
        }
        
        _id = _parentId;
        _parentId = default;
        
        if (_id.IsFlow)
        {
            AwaitOnSucceed(result);
            return;
        }

        AwaitHydrate(result);
    }

    void IDAsyncMethodBuilder.SetException(Exception exception)
    {
        AssertState<IDAsyncMethodBuilder>(FlowState.Running);

        if (_stateMachine is DTask task)
        {
            CompletedTasks.Add(_id, task);
        }

        _stateMachine = null;
        
        if (_parentId.IsDefault)
        {
            Assign(ref _dehydrateContinuation, static self => self.AwaitFlush());
            AwaitDehydrateCompleted(exception);
            return;
        }
        
        _id = _parentId;
        _parentId = default;
        
        if (_id.IsFlow)
        {
            if (exception is OperationCanceledException operationCanceledException)
            {
                AwaitOnCancel(operationCanceledException);
            }
            else
            {
                AwaitOnFail(exception);
            }

            return;
        }
        
        AwaitHydrate(exception);
    }

    void IDAsyncMethodBuilder.SetState<TStateMachine>(ref TStateMachine stateMachine)
    {
        AssertState<IDAsyncMethodBuilder>(FlowState.Running);

        AwaitDehydrate(ref stateMachine);
    }
}
