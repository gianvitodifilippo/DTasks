using StackExchange.Redis;

namespace DTasks.Storage.StackExchangeRedis;

public readonly struct RedisFlowStack : IFlowStack
{
    private const string HeapKey = "heap";

    private readonly Stack<HashEntry> _entries;

    internal RedisFlowStack(Stack<HashEntry> entries)
    {
        _entries = entries;
    }

    internal HashEntry[] GetEntries()
    {
        return [.. _entries];
    }

    ReadOnlySpan<byte> IFlowStack.PopHeap()
    {
        HashEntry entry = _entries.Pop();
        if (entry.Name.Box() is not HeapKey)
            throw new InvalidOperationException(); // TODO: Message

        ReadOnlyMemory<byte> value = entry.Value;
        return value.Span;
    }

    ReadOnlySpan<byte> IFlowStack.PopStateMachine(out bool hasNext)
    {
        HashEntry entry = _entries.Pop();
        if (!entry.Name.TryParse(out int count) || count != _entries.Count)
            throw new InvalidOperationException(); // TODO: Message

        hasNext = count != 0;
        ReadOnlyMemory<byte> value = entry.Value;
        return value.Span;
    }

    void IFlowStack.PushHeap(ReadOnlyMemory<byte> bytes)
    {
        _entries.Push(new HashEntry(
            name: HeapKey,
            value: bytes));
    }

    void IFlowStack.PushStateMachine(ReadOnlyMemory<byte> bytes)
    {
        _entries.Push(new HashEntry(
            name: _entries.Count,
            value: bytes));
    }
}
