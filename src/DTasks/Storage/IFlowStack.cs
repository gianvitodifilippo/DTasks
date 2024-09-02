namespace DTasks.Storage;

public interface IFlowStack
{
    void PushHeap(ReadOnlyMemory<byte> bytes);

    void PushStateMachine(ReadOnlyMemory<byte> bytes);

    ReadOnlyMemory<byte> PopHeap();

    ReadOnlyMemory<byte> PopStateMachine(out bool hasNext);
}