using DTasks.Hosting;

namespace DTasks.Storage;

public interface IDistributedLockProvider
{
    Task<IAsyncDisposable> LockAsync(FlowId flowId, CancellationToken cancellationToken = default);
}
