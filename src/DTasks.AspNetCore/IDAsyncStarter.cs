using DTasks.Infrastructure;

namespace DTasks.AspNetCore;

public interface IDAsyncStarter
{
    ValueTask StartAsync(IDAsyncRunnable runnable, CancellationToken cancellationToken = default);
}