using System.Collections.Concurrent;
using DTasks.Utils;

namespace DTasks.Serialization.InMemory;

public sealed class InMemoryDAsyncStorage : IDAsyncStorage
{
    private static readonly Task<Option<ReadOnlyMemory<byte>>> s_cachedNoneTask = Task.FromResult(Option<ReadOnlyMemory<byte>>.None);
    
    private readonly ConcurrentDictionary<object, ReadOnlyMemory<byte>> _storage = new();
    
    public Task<Option<ReadOnlyMemory<byte>>> LoadAsync<TKey>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        return _storage.TryGetValue(key, out ReadOnlyMemory<byte> bytes)
            ? Task.FromResult(Option.Some(bytes))
            : s_cachedNoneTask;
    }

    public Task SaveAsync<TKey>(TKey key, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        _storage[key] = bytes;
        return Task.CompletedTask;
    }
}