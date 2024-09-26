using StackExchange.Redis;

namespace DTasks.Storage.StackExchangeRedis;

public sealed class RedisDTaskStorage(IDatabase database) : IDTaskStorage<RedisFlowStack>
{
    public RedisFlowStack CreateStack()
    {
        Stack<ReadOnlyMemory<byte>> items = new();
        return new RedisFlowStack(items);
    }

    public async ValueTask<RedisFlowStack> LoadStackAsync<TFlowId>(TFlowId flowId, CancellationToken cancellationToken = default)
        where TFlowId : notnull
    {
        RedisKey key = flowId.ToString();
        RedisValue[] values = await database.ListRangeAsync(key);

        Stack<ReadOnlyMemory<byte>> items = new(values.Length);
        foreach (RedisValue value in values)
        {
            items.Push(value);
        }

        return new RedisFlowStack(items);
    }

    public Task SaveStackAsync<TFlowId>(TFlowId flowId, ref RedisFlowStack stack, CancellationToken cancellationToken = default)
        where TFlowId : notnull
    {
        Stack<ReadOnlyMemory<byte>> items = stack.Items;
        int count = items.Count;

        RedisKey key = flowId.ToString();
        RedisValue[] values = new RedisValue[count];

        for (int i = 0; i < count; i++)
        {
            values[i] = items.Pop();
        }

        return database.ListRightPushAsync(key, values);
    }

    public Task ClearStackAsync<TFlowId>(TFlowId flowId, ref RedisFlowStack stack, CancellationToken cancellationToken = default) where TFlowId : notnull
    {
        RedisKey key = flowId.ToString();
        return database.KeyDeleteAsync(key);
    }
}
