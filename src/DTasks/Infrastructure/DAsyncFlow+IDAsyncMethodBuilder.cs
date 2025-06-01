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
            // To avoid possible stack dives, invoke the continuation asynchronously.
            // Awaiting a completed Task gets the job done and saves us the bother of flowing the execution context, as the state machine box takes care of that.

            Await(Task.CompletedTask);
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

        _stateMachine = null;
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

        _stateMachine = null;
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

        _stateMachine = null;
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
