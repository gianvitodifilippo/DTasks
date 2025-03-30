using DTasks.Infrastructure;

namespace DTasks.AspNetCore;

public interface IWorkQueue
{
    Task YieldAsync(DAsyncId id, CancellationToken cancellationToken = default);

    Task DelayAsync(DAsyncId id, TimeSpan delay, CancellationToken cancellationToken = default);
}
