namespace DTasks.Storage.StackExchangeRedis;

public struct RedisFlowStack : IFlowStack
{
    private bool _isDisposed;

    internal RedisFlowStack(Stack<ReadOnlyMemory<byte>> items)
    {
        _isDisposed = false;
        Items = items;
    }

    internal readonly Stack<ReadOnlyMemory<byte>> Items { get; }

    public readonly ValueTask<ReadOnlyMemory<byte>> PopAsync(CancellationToken cancellationToken = default)
    {
        EnsureNotDisposed();

        return Items.TryPop(out ReadOnlyMemory<byte> item)
            ? new ValueTask<ReadOnlyMemory<byte>>(item)
            : default;
    }

    public readonly void Push(ReadOnlyMemory<byte> bytes)
    {
        EnsureNotDisposed();

        Items.Push(bytes);
    }

    internal void Dispose()
    {
        _isDisposed = true;
    }

    private readonly void EnsureNotDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(RedisFlowStack));
    }
}
