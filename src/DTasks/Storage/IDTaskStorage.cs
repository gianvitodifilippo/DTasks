using DTasks.Hosting;

namespace DTasks.Storage;

public interface IDTaskStorage<TStack>
    where TStack : IFlowStack
{
    TStack CreateStack();

    ValueTask<TStack> LoadStackAsync(FlowId flowId, CancellationToken cancellationToken = default);

    Task SaveStackAsync(FlowId flowId, ref TStack stack, CancellationToken cancellationToken = default);

    Task ClearStackAsync(FlowId flowId, ref TStack stack, CancellationToken cancellationToken = default);

    ValueTask<ReadOnlyMemory<byte>> LoadValueAsync(FlowId flowId, CancellationToken cancellationToken = default);

    Task SaveValueAsync(FlowId flowId, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default);

    Task ClearValueAsync(FlowId flowId, CancellationToken cancellationToken = default);
}
