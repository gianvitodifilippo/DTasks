using System.Buffers;
using DTasks.Infrastructure.State;
using DTasks.Utils;

namespace DTasks.Serialization;

public sealed class BinaryDAsyncHeap(IDAsyncSerializer serializer, IDAsyncStorage storage) : IDAsyncHeap
{
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

        return Option.Some(serializer.Deserialize<TValue>(loadResult.Value.Span));
    }

    public Task DeleteAsync<TKey>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        throw new NotImplementedException();
    }
}
