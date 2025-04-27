using DTasks.Configuration.DependencyInjection;
using DTasks.Serialization.Configuration;
using StackExchange.Redis;

namespace DTasks.Serialization.StackExchangeRedis.Configuration;

internal sealed class RedisStorageConfigurationBuilder : IRedisStorageConfigurationBuilder
{
    private IComponentDescriptor<IDatabase>? _databaseDescriptor;

    public ISerializationConfigurationBuilder Configure(ISerializationConfigurationBuilder builder)
    {
        if (_databaseDescriptor is null)
            throw new InvalidCastException("Redis storage was not properly configured.");

        var storageDescriptor = _databaseDescriptor.Map(database => new RedisDAsyncStorage(database));
        return builder.UseStorage(storageDescriptor);
    }

    IRedisStorageConfigurationBuilder IRedisStorageConfigurationBuilder.UseDatabase(IComponentDescriptor<IDatabase> descriptor)
    {
        _databaseDescriptor = descriptor;
        return this;
    }
}