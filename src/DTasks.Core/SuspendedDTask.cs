using DTasks.Host;
namespace DTasks;

internal sealed class SuspendedDTask<TResult>(ISuspensionCallback callback) : DTask<TResult>
{
    public override DTaskStatus Status => DTaskStatus.Suspended;

    internal override TResult Result
    {
        get
        {
            InvalidStatus(DTaskStatus.RanToCompletion);
            return default!;
        }
    }

    private protected override Task<bool> UnderlyingTask => Task.FromResult(false);

    internal override Task OnSuspendedAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
    {
        return handler.OnSuspendedAsync(callback, cancellationToken);
    }
}
