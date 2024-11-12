using DTasks.CompilerServices;
using DTasks.Utils;
using System.Diagnostics;

namespace DTasks.Hosting;

internal partial class DAsyncFlow : IDAsyncMethodBuilder
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

            Await(Task.CompletedTask, FlowState.Running);
        }
        else
        {
            _suspendingAwaiterOrType = typeof(TAwaiter).IsValueType
                ? typeof(TAwaiter)
                : awaiter;

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
        _id = default;
        _stateMachine = null;
        Resume(_parentId);
    }

    void IDAsyncMethodBuilder.SetResult<TResult>(TResult result)
    {
        _id = default;
        _stateMachine = null;
        Resume(_parentId, result);
    }

    void IDAsyncMethodBuilder.SetException(Exception exception)
    {
        _id = default;
        _stateMachine = null;
        Resume(_parentId, exception);
    }

    void IDAsyncMethodBuilder.SetState<TStateMachine>(ref TStateMachine stateMachine)
    {
        DAsyncId parentId = _parentId;
        DAsyncId id = _id;

        _parentId = _id;
        _id = DAsyncId.New();

        Dehydrate(parentId, id, ref stateMachine);
    }
}
