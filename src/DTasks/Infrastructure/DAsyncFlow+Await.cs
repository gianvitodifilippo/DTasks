using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
    private void AwaitOnStart()
    {
        Await(_host.OnStartAsync(this, _cancellationToken), FlowState.Starting);
    }

    private void AwaitOnSuspend()
    {
        Await(_host.OnSuspendAsync(this, _cancellationToken), FlowState.Returning);
    }

    private void AwaitOnSucceed()
    {
        Await(_host.OnSucceedAsync(this, _cancellationToken), FlowState.Returning);
    }

    private void AwaitOnSucceed<TResult>(TResult result)
    {
        Await(_host.OnSucceedAsync(this, result, _cancellationToken), FlowState.Returning);
    }

    private void AwaitOnFail(Exception exception)
    {
        Await(_host.OnFailAsync(this, exception, _cancellationToken), FlowState.Returning);
    }

    private void AwaitOnCancel(OperationCanceledException exception)
    {
        Await(_host.OnCancelAsync(this, exception, _cancellationToken), FlowState.Returning);
    }

    private void Return()
    {
        Await(Task.CompletedTask, FlowState.Returning);
    }

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
