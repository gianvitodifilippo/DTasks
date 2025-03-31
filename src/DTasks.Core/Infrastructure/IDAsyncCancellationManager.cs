using System.ComponentModel;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncCancellationManager
{
    Task CancelAsync(CancellationToken cancellationToken);

    Task CancelAsync(TimeSpan delay, CancellationToken cancellationToken);
}
