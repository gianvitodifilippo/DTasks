using DTasks.Configuration;
using DTasks.Configuration.DependencyInjection;
using DTasks.Serialization.Configuration;
using StackExchange.Redis;

namespace DTasks.Serialization.StackExchangeRedis.Configuration;

internal sealed class RedisStorageConfigurationBuilder : IRedisStorageConfigurationBuilder
{
    private IComponentDescriptor<IDatabase>? _databaseDescriptor;

    public TBuilder Configure<TBuilder>(TBuilder builder)
        where TBuilder : ISerializationConfigurationBuilder
    {
        if (_databaseDescriptor is null)
            throw new InvalidOperationException("Redis storage was not properly configured.");

        var storageDescriptor = _databaseDescriptor.Map(database => new RedisDAsyncStorage(database));
        builder.UseStorage(storageDescriptor);

        return builder;
    }

    IRedisStorageConfigurationBuilder IRedisStorageConfigurationBuilder.UseDatabase(IComponentDescriptor<IDatabase> descriptor)
    {
        _databaseDescriptor = descriptor;
        return this;
    }
}