using System.ComponentModel;
using DTasks.Execution;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncCancellationManager
{
    bool IsCancellationRequested(DCancellationTokenSource source);

    Task CreateAsync(DCancellationTokenSource source, DAsyncCancellationHandle handle, CancellationToken cancellationToken);

    Task CreateAsync(DCancellationTokenSource source, DAsyncCancellationHandle handle, TimeSpan delay, CancellationToken cancellationToken);

    Task CancelAsync(DCancellationTokenSource source, CancellationToken cancellationToken);

    Task CancelAfterAsync(DCancellationTokenSource source, TimeSpan delay, CancellationToken cancellationToken);
}
