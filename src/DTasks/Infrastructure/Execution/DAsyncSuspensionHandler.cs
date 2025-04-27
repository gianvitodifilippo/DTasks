namespace DTasks.Infrastructure.Execution;

internal static class DAsyncSuspensionHandler
{
    public static readonly IDAsyncSuspensionHandler Default = new DefaultDAsyncSuspensionHandler();
    
    private sealed class DefaultDAsyncSuspensionHandler : IDAsyncSuspensionHandler
    {
        public Task OnYieldAsync(DAsyncId id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("The current d-async host does not support yielding.");
        }

        public Task OnDelayAsync(DAsyncId id, TimeSpan delay, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("The current d-async host does not support delaying.");
        }
    }
}