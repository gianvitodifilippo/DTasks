using DTasks.Extensions.DependencyInjection.Configuration;
using StackExchange.Redis;

namespace DTasks.Serialization.Configuration;

public static class RedisDependencyInjectionSerializationConfigurationBuilderExtensions
{
    public static TBuilder UseStackExchangeRedis<TBuilder>(this TBuilder builder)
        where TBuilder : ISerializationConfigurationBuilder
    {
        return builder.UseStackExchangeRedis(redis => redis
            .UseDatabase(InfrastructureServiceProvider.GetRequiredService<IDatabase>()));
    }
}
