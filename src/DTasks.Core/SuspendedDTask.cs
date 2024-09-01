using DTasks.Hosting;

namespace DTasks;

internal sealed class SuspendedDTask<TResult>(ISuspensionCallback callback) : DTask<TResult>
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
        return handler.OnSuspendedAsync(callback, cancellationToken);
    }
}
