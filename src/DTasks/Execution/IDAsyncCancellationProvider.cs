using System.ComponentModel;

namespace DTasks.Execution;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncCancellationProvider
{
    void RegisterHandler(IDAsyncCancellationHandler handler);

    void UnregisterHandler(IDAsyncCancellationHandler handler);

    Task CancelAsync(DCancellationId id, CancellationToken cancellationToken = default);

    Task CancelAsync(DCancellationId id, DateTimeOffset expirationTime, CancellationToken cancellationToken = default);
}