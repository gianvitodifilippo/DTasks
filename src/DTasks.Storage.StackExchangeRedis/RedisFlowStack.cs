using DTasks.Hosting;
using StackExchange.Redis;
using System.Diagnostics;
using System.Text;

namespace DTasks.Storage.StackExchangeRedis;

public struct RedisFlowStack : IFlowStack
{
    private static readonly RedisValue _heapName = Encoding.UTF8.GetBytes("Heap").AsMemory();

    private readonly Stack<ReadOnlyMemory<byte>> _entries;
    private StackState _state;

    private RedisFlowStack(Stack<ReadOnlyMemory<byte>> entries, StackState state)
    {
        _state = state;
        _entries = entries;
    }

    private readonly bool IsOpen => _state is StackState.Open;

    private readonly bool IsClosed => _state is StackState.Closed;

    internal HashEntry[] ToArrayAndDispose()
    {
        EnsureNotDisposed();

        if (!IsClosed)
            throw new InvalidOperationException("Cannot save stack without pushing heap before.");

        HashEntry[] entries = new HashEntry[_entries.Count];
        ReadOnlyMemory<byte> bytes = _entries.Pop();

        entries[0] = new HashEntry(
            name: _heapName,
            value: bytes);

        int index = 0;
        while (_entries.TryPop(out bytes))
        {
            entries[++index] = new HashEntry(
                name: index,
                value: bytes);
        }

        _state = StackState.Disposed;
        return entries;
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

    public static RedisFlowStack Restore<TFlowId>(TFlowId flowId, HashEntry[] entries)
        where TFlowId : notnull
    {
        if (entries.Length < 2)
            throw new CorruptedDFlowException(flowId, "Hash did not contain enough entries.");

        Stack<ReadOnlyMemory<byte>> stack = new(entries.Length);
        for (int index = entries.Length - 1; index >= 1; index--)
        {
            HashEntry stateMachineEntry = entries[index];
            if (!IsStateMachineNameValid(index, in stateMachineEntry))
                throw new CorruptedDFlowException(flowId, "Invalid state machine name.");

            stack.Push(stateMachineEntry.Value);
        }

        HashEntry heapEntry = entries[0];
        if (!IsHeapNameValid(in heapEntry))
            throw new CorruptedDFlowException(flowId, "Invalid heap name.");

        stack.Push(heapEntry.Value);

        return new RedisFlowStack(stack, StackState.Closed);
    }

    private readonly void EnsureNotDisposed()
    {
        if (_state is StackState.Disposed)
            throw new ObjectDisposedException(nameof(IFlowStack));
    }

    private static bool IsStateMachineNameValid(int index, in HashEntry entry)
    {
        Debug.Assert(index > 0, "Index of state machine entries should be greater than 0.");

        return entry.Name.TryParse(out int key) && key == index;
    }

    private static bool IsHeapNameValid(in HashEntry entry)
    {
        return entry.Name == _heapName;
    }

    private enum StackState : byte
    {
        Open,
        Closed,
        Disposed
    }
}
