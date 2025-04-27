using DTasks.Extensions.DependencyInjection.Configuration;
using StackExchange.Redis;

namespace DTasks.Serialization.Configuration;

public static class RedisDependencyInjectionSerializationConfigurationBuilderExtensions
{
    public static ISerializationConfigurationBuilder UseStackExchangeRedis(this ISerializationConfigurationBuilder builder)
    {
        return builder.UseStackExchangeRedis(redis => redis
            .UseDatabase(InfrastructureServiceProvider.GetRequiredService<IDatabase>()));
    }
}