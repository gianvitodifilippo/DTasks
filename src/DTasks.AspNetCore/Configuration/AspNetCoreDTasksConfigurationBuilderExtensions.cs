using DTasks.AspNetCore.Configuration;
using DTasks.Metadata;

namespace DTasks.Configuration;

public static class AspNetCoreDTasksConfigurationBuilderExtensions
{
    public static TBuilder UseAspNetCore<TBuilder>(this TBuilder builder)
        where TBuilder : IDependencyInjectionDTasksConfigurationBuilder
    {
        DTasksAspNetCoreConfigurationBuilder aspNetCoreBuilder = new(builder);

        return aspNetCoreBuilder.Configure(builder);
    }

    public static TBuilder UseAspNetCore<TBuilder>(this TBuilder builder, [ConfigurationBuilder] Action<IDTasksAspNetCoreConfigurationBuilder> configure)
        where TBuilder : IDependencyInjectionDTasksConfigurationBuilder
    {
        DTasksAspNetCoreConfigurationBuilder aspNetCoreBuilder = new(builder);
        configure(aspNetCoreBuilder);

        return aspNetCoreBuilder.Configure(builder);
    }
}