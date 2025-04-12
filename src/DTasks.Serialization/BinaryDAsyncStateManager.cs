using System.Buffers;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using DTasks.Utils;

namespace DTasks.Serialization;

public sealed class BinaryDAsyncStateManager(IDAsyncSerializer serializer, IDAsyncStorage storage) : IDAsyncStateManager, IDAsyncStack, IDAsyncHeap
{
    IDAsyncStack IDAsyncStateManager.Stack => this;

    IDAsyncHeap IDAsyncStateManager.Heap => this;

    public ValueTask DehydrateAsync<TStateMachine>(ISuspensionContext context, DAsyncId parentId, DAsyncId id, ref TStateMachine stateMachine, CancellationToken cancellationToken = default)
        where TStateMachine : notnull
    {
        ArrayBufferWriter<byte> buffer = new();
        serializer.SerializeStateMachine(buffer, context, parentId, ref stateMachine);
        ReadOnlyMemory<byte> memory = buffer.WrittenMemory;
        return new(storage.SaveAsync(id, memory, cancellationToken));
    }

    public async ValueTask<DAsyncLink> HydrateAsync(IResumptionContext context, DAsyncId id, CancellationToken cancellationToken = default)
    {
        Option<ReadOnlyMemory<byte>> loadResult = await storage.LoadAsync(id, cancellationToken);
        if (!loadResult.HasValue)
            throw new InvalidOperationException("Invalid id."); // TODO: Improve error message
        
        return serializer.DeserializeStateMachine(context, loadResult.Value.Span);
    }

    public async ValueTask<DAsyncLink> HydrateAsync<TResult>(IResumptionContext context, DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        Option<ReadOnlyMemory<byte>> loadResult = await storage.LoadAsync(id, cancellationToken);
        if (!loadResult.HasValue)
            throw new InvalidOperationException("Invalid id.");

        return serializer.DeserializeStateMachine(context, loadResult.Value.Span, result);
    }

    public async ValueTask<DAsyncLink> HydrateAsync(IResumptionContext context, DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        Option<ReadOnlyMemory<byte>> loadResult = await storage.LoadAsync(id, cancellationToken);
        if (!loadResult.HasValue)
            throw new InvalidOperationException("Invalid id.");

        return serializer.DeserializeStateMachine(context, loadResult.Value.Span, exception);
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

    public Task SaveAsync<TKey, TValue>(TKey key, TValue value, CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        ArrayBufferWriter<byte> buffer = new();
        serializer.Serialize(buffer, value);
        ReadOnlyMemory<byte> memory = buffer.WrittenMemory;
        return storage.SaveAsync(key, memory, cancellationToken);
    }

    public async Task<Option<TValue>> LoadAsync<TKey, TValue>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        Option<ReadOnlyMemory<byte>> loadResult = await storage.LoadAsync(key, cancellationToken);
        if (!loadResult.HasValue)
            return Option<TValue>.None;
        
        return Option<TValue>.Some(serializer.Deserialize<TValue>(loadResult.Value.Span));
    }

    public Task DeleteAsync<TKey>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        throw new NotImplementedException();
    }
}
