namespace DTasks.Infrastructure.Execution;

public abstract class DAsyncSuspensionHandler : IDAsyncSuspensionHandler
{
    public static readonly IDAsyncSuspensionHandler Default = new DefaultDAsyncSuspensionHandler();

    private DAsyncSuspensionHandler()
    {
    }

    public abstract Task OnYieldAsync(DAsyncId id, CancellationToken cancellationToken);

    public abstract Task OnDelayAsync(DAsyncId id, TimeSpan delay, CancellationToken cancellationToken);
    
    private sealed class DefaultDAsyncSuspensionHandler : DAsyncSuspensionHandler
    {
        public override Task OnYieldAsync(DAsyncId id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("The current d-async host does not support yielding.");
        }

        public override Task OnDelayAsync(DAsyncId id, TimeSpan delay, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("The current d-async host does not support delaying.");
        }
    }
}