namespace DTasks.Infrastructure.Execution;

public abstract class DAsyncCancellationProvider : IDAsyncCancellationProvider
{
    public static readonly IDAsyncCancellationProvider Default = new DefaultDAsyncCancellationProvider();

    private DAsyncCancellationProvider()
    {
    }

    public abstract void RegisterHandler(IDAsyncCancellationHandler handler);
    
    public abstract void UnregisterHandler(IDAsyncCancellationHandler handler);
    
    public abstract Task CancelAsync(DCancellationId id, CancellationToken cancellationToken = default);
    
    public abstract Task CancelAsync(DCancellationId id, DateTimeOffset expirationTime, CancellationToken cancellationToken = default);

    private sealed class DefaultDAsyncCancellationProvider : DAsyncCancellationProvider
    {
        public override Task CancelAsync(DCancellationId id, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("The current d-async host does not support distributed cancellation.");
        }

        public override Task CancelAsync(DCancellationId id, DateTimeOffset expirationTime, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("The current d-async host does not support distributed cancellation.");
        }

        public override void RegisterHandler(IDAsyncCancellationHandler handler)
        {
        }

        public override void UnregisterHandler(IDAsyncCancellationHandler handler)
        {
        }
    }
}
