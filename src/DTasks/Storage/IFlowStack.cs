namespace DTasks.Storage;

public interface IFlowStack
{
    void Push(ReadOnlyMemory<byte> bytes);

    ValueTask<ReadOnlyMemory<byte>> PopAsync(CancellationToken cancellationToken = default);
}
