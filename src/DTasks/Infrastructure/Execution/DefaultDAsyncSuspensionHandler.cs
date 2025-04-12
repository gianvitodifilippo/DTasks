namespace DTasks.Infrastructure.Execution;

internal sealed class DefaultDAsyncSuspensionHandler : IDAsyncSuspensionHandler
{
    public static readonly DefaultDAsyncSuspensionHandler Instance = new();

    private DefaultDAsyncSuspensionHandler()
    {
    }

    public Task OnYieldAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("The current d-async host does not support yielding.");
    }

    public Task OnDelayAsync(DAsyncId id, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("The current d-async host does not support delaying.");
    }
}