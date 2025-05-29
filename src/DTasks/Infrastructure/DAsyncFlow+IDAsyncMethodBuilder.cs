using DTasks.Utils;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncMethodBuilder
{
    void IDAsyncMethodBuilder.AwaitOnCompleted<TAwaiter>(ref TAwaiter awaiter)
    {
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

            _state = FlowState.Running;
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
            catch
            {
                _suspendingAwaiterOrType = null;
                throw;
            }
        }
    }

    void IDAsyncMethodBuilder.SetResult()
    {
        ResumeParent();
    }

    void IDAsyncMethodBuilder.SetResult<TResult>(TResult result)
    {
        ResumeParent(result);
    }

    void IDAsyncMethodBuilder.SetException(Exception exception)
    {
        if (exception is OperationCanceledException operationCanceledException)
        {
            ResumeParent(operationCanceledException);
        }
        else
        {
            ResumeParent(exception);
        }
    }

    void IDAsyncMethodBuilder.SetState<TStateMachine>(ref TStateMachine stateMachine)
    {
        throw new NotImplementedException();
        // DAsyncId parentId = _parentId;
        // DAsyncId id = _id;
        //
        // _parentId = _id;
        // _id = DAsyncId.New();
        //
        // Dehydrate(parentId, id, ref stateMachine);
    }
}
