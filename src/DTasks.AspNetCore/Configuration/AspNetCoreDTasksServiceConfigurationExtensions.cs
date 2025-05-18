using DTasks.AspNetCore.Configuration;
using DTasks.AspNetCore.Infrastructure.Http;
using DTasks.Serialization.Configuration;
using DTasks.Serialization.Json.Converters;

namespace DTasks.Configuration;

public static class AspNetCoreDTasksServiceConfigurationExtensions
{
    public static TBuilder UseAspNetCore<TBuilder>(this TBuilder builder)
        where TBuilder : IDependencyInjectionDTasksConfigurationBuilder
    {
        DTasksAspNetCoreConfigurationBuilder aspNetCoreBuilder = new(builder);

        return aspNetCoreBuilder.Configure(builder);
    }

    public static TBuilder UseAspNetCore<TBuilder>(this TBuilder builder, Action<IDTasksAspNetCoreConfigurationBuilder> configure)
        where TBuilder : IDependencyInjectionDTasksConfigurationBuilder
    {
        DTasksAspNetCoreConfigurationBuilder aspNetCoreBuilder = new(builder);
        configure(aspNetCoreBuilder);

        return aspNetCoreBuilder.Configure(builder);
    }
}