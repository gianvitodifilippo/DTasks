using DTasks.Hosting;

namespace DTasks;

internal sealed class DelegateSuspendedDTask<TResult>(SuspensionCallback callback) : DTask<TResult>, ISuspensionCallback
{
    internal override DTaskStatus Status => DTaskStatus.Suspended;

    internal override TResult Result
    {
        get
        {
            InvalidStatus(expectedStatus: DTaskStatus.RanToCompletion);
            return default!;
        }
    }

    internal override Task<bool> UnderlyingTask => Task.FromResult(false);

    internal override Task SuspendAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
    {
        return handler.OnCallbackAsync(this, cancellationToken);
    }

    Task ISuspensionCallback.OnSuspendedAsync<TFlowId>(TFlowId flowId, CancellationToken cancellationToken)
    {
        return callback(flowId, cancellationToken);
    }
}

internal sealed class DelegateSuspendedDTask<TResult, TState>(TState state, SuspensionCallback<TState> callback) : DTask<TResult>, ISuspensionCallback<TState>
{
    internal override DTaskStatus Status => DTaskStatus.Suspended;

    internal override TResult Result
    {
        get
        {
            InvalidStatus(expectedStatus: DTaskStatus.RanToCompletion);
            return default!;
        }
    }

    internal override Task<bool> UnderlyingTask => Task.FromResult(false);

    internal override Task SuspendAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
    {
        return handler.OnCallbackAsync(state, this, cancellationToken);
    }

    Task ISuspensionCallback<TState>.OnSuspendedAsync<TFlowId>(TFlowId flowId, TState state, CancellationToken cancellationToken)
    {
        return callback(flowId, state, cancellationToken);
    }
}
