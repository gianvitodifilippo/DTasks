using DTasks.Hosting;
using StackExchange.Redis;

namespace DTasks.Storage.StackExchangeRedis;

public struct RedisFlowStack : IFlowStack
{
    private readonly Stack<ReadOnlyMemory<byte>> _entries;
    private StackState _state;

    private RedisFlowStack(Stack<ReadOnlyMemory<byte>> entries, StackState state)
    {
        _state = state;
        _entries = entries;
    }

    private readonly bool IsOpen => _state is StackState.Open;

    private readonly bool IsClosed => _state is StackState.Closed;

    internal RedisValue[] ToArrayAndDispose()
    {
        EnsureNotDisposed();

        if (!IsClosed)
            throw new InvalidOperationException("Cannot save stack without pushing heap before.");

        int count = _entries.Count;
        RedisValue[] items = new RedisValue[count];

        int index = count;
        while (_entries.TryPop(out ReadOnlyMemory<byte> bytes))
        {
            items[--index] = bytes;
        }

        _state = StackState.Disposed;
        return items;
    }

    public ReadOnlySpan<byte> PopHeap()
    {
        EnsureNotDisposed();

        if (!IsClosed)
            throw new InvalidOperationException("The heap was already popped.");

        _state = StackState.Open;
        ReadOnlyMemory<byte> value = _entries.Pop();
        return value.Span;
    }

    public readonly ReadOnlySpan<byte> PopStateMachine(out bool hasNext)
    {
        EnsureNotDisposed();

        if (!IsOpen)
            throw new InvalidOperationException("Cannot pop state machine since the heap was already pushed.");

        if (_entries.Count == 0)
            throw new InvalidOperationException("The stack is empty.");

        ReadOnlyMemory<byte> value = _entries.Pop();
        hasNext = _entries.Count != 0;
        return value.Span;
    }

    public void PushHeap(ReadOnlyMemory<byte> bytes)
    {
        EnsureNotDisposed();

        if (!IsOpen)
            throw new InvalidOperationException("Cannot push heap since the heap was already pushed.");

        if (_entries.Count == 0)
            throw new InvalidOperationException("At least one state machine must be pushed before pushing the heap.");

        _state = StackState.Closed;
        _entries.Push(bytes);
    }

    public readonly void PushStateMachine(ReadOnlyMemory<byte> bytes)
    {
        EnsureNotDisposed();

        if (!IsOpen)
            throw new InvalidOperationException("Cannot push state machine since the heap was already pushed.");

        _entries.Push(bytes);
    }

    public static RedisFlowStack Create() => new([], StackState.Open);

    public static RedisFlowStack Restore<TFlowId>(TFlowId flowId, RedisValue[] items)
        where TFlowId : notnull
    {
        if (items.Length == 0)
            throw new CorruptedDFlowException(flowId, $"No data found for the d-async flow '{flowId}'. This may indicate that the flow id is invalid or the stored data has been deleted.");

        if (items.Length == 1)
            throw new CorruptedDFlowException(flowId, $"Expected at least 2 Redis items relative to the d-async flow '{flowId}', but found only 1.");

        Stack<ReadOnlyMemory<byte>> stack = new(items.Length);
        foreach (RedisValue item in items)
        {
            stack.Push(item);
        }

        return new RedisFlowStack(stack, StackState.Closed);
    }

    private readonly void EnsureNotDisposed()
    {
        if (_state is StackState.Disposed)
            throw new ObjectDisposedException(nameof(IFlowStack));
    }

    private enum StackState : byte
    {
        Open,
        Closed,
        Disposed
    }
}
