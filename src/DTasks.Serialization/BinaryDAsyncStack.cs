using System.Buffers;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using DTasks.Utils;

namespace DTasks.Serialization;

public sealed class BinaryDAsyncStack(IStateMachineSerializer stateMachineSerializer, IDAsyncStorage storage) : IDAsyncStack
{
    public ValueTask DehydrateAsync<TStateMachine>(ISuspensionContext context, DAsyncId parentId, DAsyncId id, ref TStateMachine stateMachine, CancellationToken cancellationToken = default)
        where TStateMachine : notnull
    {
        ArrayBufferWriter<byte> buffer = new();
        stateMachineSerializer.SerializeStateMachine(buffer, context, parentId, ref stateMachine);
        ReadOnlyMemory<byte> memory = buffer.WrittenMemory;
        return new(storage.SaveAsync(id, memory, cancellationToken));
    }

    public async ValueTask<DAsyncLink> HydrateAsync(IResumptionContext context, DAsyncId id, CancellationToken cancellationToken = default)
    {
        Option<ReadOnlyMemory<byte>> loadResult = await storage.LoadAsync(id, cancellationToken);
        if (!loadResult.HasValue)
            throw new InvalidOperationException("Invalid id."); // TODO: Improve error message
        
        return stateMachineSerializer.DeserializeStateMachine(context, loadResult.Value.Span);
    }

    public async ValueTask<DAsyncLink> HydrateAsync<TResult>(IResumptionContext context, DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        Option<ReadOnlyMemory<byte>> loadResult = await storage.LoadAsync(id, cancellationToken);
        if (!loadResult.HasValue)
            throw new InvalidOperationException("Invalid id.");

        return stateMachineSerializer.DeserializeStateMachine(context, loadResult.Value.Span, result);
    }

    public async ValueTask<DAsyncLink> HydrateAsync(IResumptionContext context, DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        Option<ReadOnlyMemory<byte>> loadResult = await storage.LoadAsync(id, cancellationToken);
        if (!loadResult.HasValue)
            throw new InvalidOperationException("Invalid id.");

        return stateMachineSerializer.DeserializeStateMachine(context, loadResult.Value.Span, exception);
    }

    public ValueTask<DAsyncId> DeleteAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        // TODO
        return default;
    }
}