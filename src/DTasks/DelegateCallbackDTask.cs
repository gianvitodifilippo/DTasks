using DTasks.Hosting;

namespace DTasks;

internal sealed class DelegateCallbackDTask<TResult>(SuspensionCallback callback) : DTask<TResult>, ISuspensionCallback
{
    public override DTaskStatus Status => DTaskStatus.Suspended;

    public Task InvokeAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        return callback(id, cancellationToken);
    }

    protected override void Run(IDAsyncFlow flow)
    {
        if (flow is not IDAsyncFlowInternal flowInternal)
            throw new ArgumentException("The provided flow does not support callbacks.", nameof(flow));

        flowInternal.Callback(this);
    }
}

internal sealed class DelegateCallbackDTask<TResult, TState>(TState state, SuspensionCallback<TState> callback) : DTask<TResult>, ISuspensionCallback
{
    public override DTaskStatus Status => DTaskStatus.Suspended;

    public Task InvokeAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        return callback(id, state, cancellationToken);
    }

    protected override void Run(IDAsyncFlow flow)
    {
        if (flow is not IDAsyncFlowInternal flowInternal)
            throw new ArgumentException("The provided flow does not support callbacks.", nameof(flow));

        flowInternal.Callback(this);
    }
}
