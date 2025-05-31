using DTasks.Utils;
using StackExchange.Redis;

namespace DTasks.Serialization.StackExchangeRedis;

public sealed class RedisDAsyncStorage(IDatabase database) : IDAsyncStorage
{
    public async Task<Option<ReadOnlyMemory<byte>>> LoadAsync<TKey>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        RedisValue value = await database.StringGetAsync(key.ToString());

        return value.HasValue
            ? Option<ReadOnlyMemory<byte>>.Some(value)
            : Option<ReadOnlyMemory<byte>>.None;
    }

    public async Task SaveAsync<TKey>(TKey key, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        await database.StringSetAsync(key.ToString(), bytes);
    }

    public async Task DeleteAsync<TKey>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        await database.KeyDeleteAsync(key.ToString());
    }
}
