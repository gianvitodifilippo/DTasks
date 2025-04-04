
namespace DTasks.Execution;

internal sealed class DefaultDistributedCancellationProvider : IDistributedCancellationProvider
{
    public static readonly DefaultDistributedCancellationProvider Instance = new();

    private DefaultDistributedCancellationProvider()
    {
    }

    public Task CancelAsync(DCancellationId id, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("The current d-async host does not support distributed cancellation.");
    }

    public Task CancelAsync(DCancellationId id, DateTimeOffset expirationTime, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("The current d-async host does not support distributed cancellation.");
    }

    public void RegisterHandler(IDistributedCancellationHandler handler)
    {
    }

    public void UnregisterHandler(IDistributedCancellationHandler handler)
    {
    }
}