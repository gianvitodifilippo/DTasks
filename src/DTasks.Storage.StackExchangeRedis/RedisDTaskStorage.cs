using StackExchange.Redis;

namespace DTasks.Storage.StackExchangeRedis;

public sealed class RedisDTaskStorage(IDatabase database) : IDTaskStorage<RedisFlowStack>
{
    public RedisFlowStack CreateStack()
    {
        return RedisFlowStack.Create();
    }

    public async Task<RedisFlowStack> LoadStackAsync<TFlowId>(TFlowId flowId, CancellationToken cancellationToken = default)
        where TFlowId : notnull
    {
        RedisKey key = flowId.ToString();
        HashEntry[] entries = await database.HashGetAllAsync(key);

        return RedisFlowStack.Restore(flowId, entries);
    }

    public Task SaveStackAsync<TFlowId>(TFlowId flowId, ref RedisFlowStack stack, CancellationToken cancellationToken = default)
        where TFlowId : notnull
    {
        RedisKey key = flowId.ToString();
        HashEntry[] entries = stack.ToArrayAndDispose();

        return database.HashSetAsync(key, entries);
    }
}
