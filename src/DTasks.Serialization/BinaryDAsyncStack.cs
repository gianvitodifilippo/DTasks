using DTasks.Infrastructure;
using System.Buffers;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Serialization;

public sealed class BinaryDAsyncStack(IDAsyncSerializer serializer, IDAsyncStorage storage) : IDAsyncStack
{
    public ValueTask DehydrateAsync<TStateMachine>(DAsyncId parentId, DAsyncId id, ref TStateMachine stateMachine, ISuspensionContext suspensionContext, CancellationToken cancellationToken = default)
        where TStateMachine : notnull
    {
        ArrayBufferWriter<byte> buffer = new();
        serializer.SerializeStateMachine(buffer, parentId, ref stateMachine, suspensionContext);
        ReadOnlyMemory<byte> memory = buffer.WrittenMemory;
        return new(storage.SaveAsync(id, memory, cancellationToken));
    }

    public async ValueTask<DAsyncLink> HydrateAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        ReadOnlyMemory<byte> bytes = await storage.LoadAsync(id, cancellationToken);
        return serializer.DeserializeStateMachine(bytes.Span);
    }

    public async ValueTask<DAsyncLink> HydrateAsync<TResult>(DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        ReadOnlyMemory<byte> bytes = await storage.LoadAsync(id, cancellationToken);
        return serializer.DeserializeStateMachine(bytes.Span, result);
    }

    public async ValueTask<DAsyncLink> HydrateAsync(DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        ReadOnlyMemory<byte> bytes = await storage.LoadAsync(id, cancellationToken);
        return serializer.DeserializeStateMachine(bytes.Span, exception);
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
