namespace DTasks.Storage;

public interface IFlowStack
{
    void PushHeap(ReadOnlySpan<byte> bytes);

    void PushStateMachine(ReadOnlySpan<byte> bytes);

    ReadOnlySpan<byte> PopHeap();

    ReadOnlySpan<byte> PopStateMachine(out bool hasNext);
}
