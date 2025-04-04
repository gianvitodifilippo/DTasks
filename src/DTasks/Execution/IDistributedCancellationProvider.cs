using System.ComponentModel;

namespace DTasks.Execution;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDistributedCancellationProvider
{
    void RegisterHandler(IDistributedCancellationHandler handler);

    void UnregisterHandler(IDistributedCancellationHandler handler);

    Task CancelAsync(DCancellationId id, CancellationToken cancellationToken = default);

    Task CancelAsync(DCancellationId id, DateTimeOffset expirationTime, CancellationToken cancellationToken = default);
}