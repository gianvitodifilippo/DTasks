namespace DTasks.Hosting;

internal partial class DAsyncFlow
{
    private void Await(Task task, FlowState state)
    {
        _state = state;

        var self = this;
        _voidTa = task.GetAwaiter();
        _builder.AwaitUnsafeOnCompleted(ref _voidTa, ref self);
    }

    private void Await(ValueTask task, FlowState state)
    {
        _state = state;

        var self = this;
        _voidVta = task.GetAwaiter();
        _builder.AwaitUnsafeOnCompleted(ref _voidVta, ref self);
    }

    private void Await(ValueTask<DAsyncLink> task, FlowState state)
    {
        _state = state;

        var self = this;
        _linkVta = task.GetAwaiter();
        _builder.AwaitUnsafeOnCompleted(ref _linkVta, ref self);
    }

    private void GetVoidTaskResult()
    {
        Consume(ref _voidTa).GetResult();
    }

    private void GetVoidValueTaskResult()
    {
        Consume(ref _voidVta).GetResult();
    }

    private DAsyncLink GetLinkValueTaskResult()
    {
        return Consume(ref _linkVta).GetResult();
    }
}
