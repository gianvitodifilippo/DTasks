using DTasks.Serialization.StackExchangeRedis.Configuration;

namespace DTasks.Configuration;

public static class RedisSerializationConfigurationBuilderExtensions
{
    public static TBuilder UseStackExchangeRedis<TBuilder>(this TBuilder builder, Action<IRedisStorageConfigurationBuilder> configure)
        where TBuilder : ISerializationConfigurationBuilder
    {
        RedisStorageConfigurationBuilder redisStorageBuilder = new();
        configure(redisStorageBuilder);

        return redisStorageBuilder.Configure(builder);
    }
}