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
        RedisValue[] items = await database.ListRangeAsync(key);

        return RedisFlowStack.Restore(flowId, items);
    }

    public Task SaveStackAsync<TFlowId>(TFlowId flowId, ref RedisFlowStack stack, CancellationToken cancellationToken = default)
        where TFlowId : notnull
    {
        RedisKey key = flowId.ToString();
        RedisValue[] items = stack.ToArrayAndDispose();

        return database.ListRightPushAsync(key, items);
    }
}
