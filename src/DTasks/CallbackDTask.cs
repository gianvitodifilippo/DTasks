using DTasks.Hosting;

namespace DTasks;

internal sealed class CallbackDTask<TResult, TCallback>(TCallback callback) : DTask<TResult>, ISuspensionCallback
    where TCallback : ISuspensionCallback
{
    public override DTaskStatus Status => DTaskStatus.Suspended;

    public Task InvokeAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        return callback.InvokeAsync(id, cancellationToken);
    }

    protected override void Run(IDAsyncFlow flow)
    {
        if (flow is not IDAsyncFlowInternal flowInternal)
            throw new ArgumentException("The provided flow does not support callbacks.", nameof(flow));

        flowInternal.Callback(this);
    }
}

internal sealed class CallbackDTask<TResult, TState, TCallback>(TState state, TCallback callback) : DTask<TResult>, ISuspensionCallback
    where TCallback : ISuspensionCallback<TState>
{
    public override DTaskStatus Status => DTaskStatus.Suspended;

    public Task InvokeAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        return callback.InvokeAsync(id, state, cancellationToken);
    }

    protected override void Run(IDAsyncFlow flow)
    {
        if (flow is not IDAsyncFlowInternal flowInternal)
            throw new ArgumentException("The provided flow does not support callbacks.", nameof(flow));

        flowInternal.Callback(this);
    }
}
