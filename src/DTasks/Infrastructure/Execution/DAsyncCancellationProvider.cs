namespace DTasks.Infrastructure.Execution;

internal static class DAsyncCancellationProvider
{
    public static readonly IDAsyncCancellationProvider Default = new DefaultDAsyncCancellationProvider();

    private sealed class DefaultDAsyncCancellationProvider : IDAsyncCancellationProvider
    {
        public Task CancelAsync(DCancellationId id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("The current d-async host does not support distributed cancellation.");
        }

        public Task CancelAsync(DCancellationId id, DateTimeOffset expirationTime, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("The current d-async host does not support distributed cancellation.");
        }

        public void RegisterHandler(IDAsyncCancellationHandler handler)
        {
        }

        public void UnregisterHandler(IDAsyncCancellationHandler handler)
        {
        }
    }
}
