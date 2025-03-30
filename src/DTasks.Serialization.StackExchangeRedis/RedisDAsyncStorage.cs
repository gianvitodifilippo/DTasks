using DTasks.Infrastructure;
using StackExchange.Redis;

namespace DTasks.Serialization.StackExchangeRedis;

public sealed class RedisDAsyncStorage(IDatabase database) : IDAsyncStorage
{
    public async Task<ReadOnlyMemory<byte>> LoadAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        RedisKey key = id.ToString();
        return await database.StringGetAsync(key);
    }

    public async Task SaveAsync(DAsyncId id, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
    {
        RedisKey key = id.ToString();
        await database.StringSetAsync(key, bytes);
    }
}
