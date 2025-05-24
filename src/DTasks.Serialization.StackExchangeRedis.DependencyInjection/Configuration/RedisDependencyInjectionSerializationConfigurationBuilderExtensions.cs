using DTasks.Configuration.DependencyInjection;
using DTasks.Extensions.DependencyInjection.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace DTasks.Configuration;

public static class RedisDependencyInjectionSerializationConfigurationBuilderExtensions
{
    public static TBuilder UseStackExchangeRedis<TBuilder>(this TBuilder builder)
        where TBuilder : ISerializationConfigurationBuilder
    {
        return builder.UseStackExchangeRedis(redis => redis
            .UseDatabase(InfrastructureServiceProvider.Descriptor.MapAsTransient(sp => sp
                .GetRequiredService<ConnectionMultiplexer>()
                .GetDatabase())));
    }
}
