using System.ComponentModel;

namespace DTasks.Infrastructure.Execution;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncSuspensionHandler
{
    Task OnYieldAsync(DAsyncId id, CancellationToken cancellationToken);

    Task OnDelayAsync(DAsyncId id, TimeSpan delay, CancellationToken cancellationToken);
}