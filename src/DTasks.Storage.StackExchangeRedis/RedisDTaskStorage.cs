using DTasks.Hosting;
using StackExchange.Redis;

namespace DTasks.Storage.StackExchangeRedis;

public sealed class RedisDTaskStorage(IDatabase database) : IDTaskStorage<RedisFlowStack>
{
    public RedisFlowStack CreateStack()
    {
        Stack<ReadOnlyMemory<byte>> items = new();
        return new RedisFlowStack(items);
    }

    public async ValueTask<RedisFlowStack> LoadStackAsync(FlowId flowId, CancellationToken cancellationToken = default)
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

    public Task SaveStackAsync(FlowId flowId, ref RedisFlowStack stack, CancellationToken cancellationToken = default)
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

    public Task ClearStackAsync(FlowId flowId, ref RedisFlowStack stack, CancellationToken cancellationToken = default)
    {
        return KeyDeleteAsync(flowId);
    }

    public async ValueTask<ReadOnlyMemory<byte>> LoadValueAsync(FlowId flowId, CancellationToken cancellationToken = default)
    {
        RedisKey key = flowId.ToString();
        RedisValue value = await database.StringGetAsync(key);

        return value;
    }

    public async Task SaveValueAsync(FlowId flowId, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
    {
        RedisKey key = flowId.ToString();
        await database.StringSetAsync(key, bytes);
    }

    public Task ClearValueAsync(FlowId flowId, CancellationToken cancellationToken = default)
    {
        return KeyDeleteAsync(flowId);
    }

    private Task KeyDeleteAsync(FlowId flowId)
    {
        RedisKey key = flowId.ToString();
        return database.KeyDeleteAsync(key);
    }
}
