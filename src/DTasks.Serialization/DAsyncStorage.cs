using System.Collections.Concurrent;
using DTasks.Utils;

namespace DTasks.Serialization;

internal abstract class DAsyncStorage : IDAsyncStorage
{
    public static readonly DAsyncStorage Default = new DefaultDAsyncStorage();

    private DAsyncStorage()
    {
    }

    public abstract Task<Option<ReadOnlyMemory<byte>>> LoadAsync<TKey>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull;

    public abstract Task SaveAsync<TKey>(TKey key, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
        where TKey : notnull;

    public abstract Task DeleteAsync<TKey>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull;

    private sealed class DefaultDAsyncStorage : DAsyncStorage
    {
        private static readonly Task<Option<ReadOnlyMemory<byte>>> s_cachedNoneTask = Task.FromResult(Option<ReadOnlyMemory<byte>>.None);

        private readonly ConcurrentDictionary<object, ReadOnlyMemory<byte>> _storage = new();

        public override Task<Option<ReadOnlyMemory<byte>>> LoadAsync<TKey>(TKey key, CancellationToken cancellationToken = default)
        {
            return _storage.TryGetValue(key, out ReadOnlyMemory<byte> bytes)
                ? Task.FromResult(Option.Some(bytes))
                : s_cachedNoneTask;
        }

        public override Task SaveAsync<TKey>(TKey key, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
        {
            _storage[key] = bytes;
            return Task.CompletedTask;
        }

        public override Task DeleteAsync<TKey>(TKey key, CancellationToken cancellationToken = default)
        {
            _storage.TryRemove(key, out _);
            return Task.CompletedTask;
        }
    }
}
