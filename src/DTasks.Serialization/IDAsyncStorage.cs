using DTasks.Utils;

namespace DTasks.Serialization;

public interface IDAsyncStorage
{
    Task<Option<ReadOnlyMemory<byte>>> LoadAsync<TKey>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull;

    Task SaveAsync<TKey>(TKey key, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
        where TKey : notnull;
}
