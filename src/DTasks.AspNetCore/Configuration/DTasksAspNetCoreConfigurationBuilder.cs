using DTasks.AspNetCore.Infrastructure.Http;
using DTasks.Configuration;
using DTasks.Serialization.Configuration;
using DTasks.Serialization.Json.Converters;

namespace DTasks.AspNetCore.Configuration;

internal sealed class DTasksAspNetCoreConfigurationBuilder : IDTasksAspNetCoreConfigurationBuilder
{
    private readonly List<Action<IAspNetCoreSerializationConfigurationBuilder>> _configureSerializationActions = [];

    public TBuilder Configure<TBuilder>(TBuilder builder)
        where TBuilder : IDependencyInjectionDTasksConfigurationBuilder
    {
        return builder
            .AddAspNetCore()
            .UseSerialization(serialization =>
            {
                AspNetCoreSerializationConfigurationBuilder aspNetCoreSerialization = new(serialization);
                foreach (var action in _configureSerializationActions)
                {
                    action (aspNetCoreSerialization);
                }

                aspNetCoreSerialization.Configure();
            });
    }

    IDTasksAspNetCoreConfigurationBuilder IDTasksAspNetCoreConfigurationBuilder.ConfigureSerialization(Action<IAspNetCoreSerializationConfigurationBuilder> configure)
    {
        _configureSerializationActions.Add(configure);
        return this;
    }
}
