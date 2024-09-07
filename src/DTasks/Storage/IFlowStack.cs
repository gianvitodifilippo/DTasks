namespace DTasks.Storage;

public interface IFlowStack
{
    void PushHeap(ReadOnlyMemory<byte> bytes);

    void PushStateMachine(ReadOnlyMemory<byte> bytes);

    ReadOnlySpan<byte> PopHeap();

    ReadOnlySpan<byte> PopStateMachine(out bool hasNext);
}
