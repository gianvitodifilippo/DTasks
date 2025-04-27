using DTasks.Serialization.StackExchangeRedis.Configuration;

namespace DTasks.Serialization.Configuration;

public static class RedisSerializationConfigurationBuilderExtensions
{
    public static ISerializationConfigurationBuilder UseStackExchangeRedis(this ISerializationConfigurationBuilder builder, Action<IRedisStorageConfigurationBuilder> configure)
    {
        RedisStorageConfigurationBuilder redisStorageBuilder = new();
        configure(redisStorageBuilder);

        return redisStorageBuilder.Configure(builder);
    }
}