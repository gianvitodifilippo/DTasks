using StackExchange.Redis;

namespace DTasks.Storage.StackExchangeRedis;

public sealed class RedisDTaskStorage(IDatabase database) : IDTaskStorage<RedisFlowStack>
{
    public RedisFlowStack CreateStack()
    {
        return new RedisFlowStack([]);
    }

    public async Task<RedisFlowStack> LoadStackAsync<TFlowId>(TFlowId flowId, CancellationToken cancellationToken)
      where TFlowId : notnull
    {
        HashEntry[] entries = await database.HashGetAllAsync(new RedisKey(flowId.ToString()));
        return new RedisFlowStack(new Stack<HashEntry>(entries));
    }

    public Task SaveStackAsync<TFlowId>(TFlowId flowId, ref RedisFlowStack stack, CancellationToken cancellationToken)
      where TFlowId : notnull
    {
        return database.HashSetAsync(flowId.ToString(), stack.GetEntries());
    }
}
