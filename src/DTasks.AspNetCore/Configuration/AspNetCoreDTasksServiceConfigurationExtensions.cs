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
        return builder
            .AddAspNetCore()
            .UseSerialization(serialization => serialization
                .UseJsonFormat(json => json
                    .ConfigureSerializerOptions((options, configuration) =>
                    {
                        options.Converters.Add(new TypedInstanceJsonConverter<object>(configuration.TypeResolver));
                        options.Converters.Add(new TypedInstanceJsonConverter<IDAsyncContinuationSurrogate>(configuration.TypeResolver));
                    })));
    }

    public static TBuilder UseAspNetCore<TBuilder>(this TBuilder builder, Action<IAspNetCoreConfigurationBuilder> configure)
        where TBuilder : IDependencyInjectionDTasksConfigurationBuilder
    {
        AspNetCoreConfigurationBuilder aspNetCoreBuilder = new();
        configure(aspNetCoreBuilder);

        return aspNetCoreBuilder.Configure(builder);
    }
}