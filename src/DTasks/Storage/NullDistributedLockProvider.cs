using DTasks.Hosting;

namespace DTasks.Storage;

public sealed class NullDistributedLockProvider : IDistributedLockProvider
{
    public static readonly NullDistributedLockProvider Instance = new();

    private readonly Task<IAsyncDisposable> _nullLockTask;

    private NullDistributedLockProvider()
    {
        _nullLockTask = Task.FromResult(new NullLock() as IAsyncDisposable);
    }

    public Task<IAsyncDisposable> LockAsync(FlowId flowId, CancellationToken cancellationToken = default)
    {
        return _nullLockTask;
    }

    private sealed class NullLock : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => default;
    }
}
