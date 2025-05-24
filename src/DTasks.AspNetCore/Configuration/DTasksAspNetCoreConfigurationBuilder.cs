using DTasks.AspNetCore.Execution;
using DTasks.Configuration;
using Microsoft.Extensions.Options;

namespace DTasks.AspNetCore.Configuration;

internal sealed class DTasksAspNetCoreConfigurationBuilder(IDependencyInjectionDTasksConfigurationBuilder dTasks) : IDTasksAspNetCoreConfigurationBuilder
{
    private readonly List<Action<IAspNetCoreSerializationConfigurationBuilder>> _configureSerializationActions = [];
    private readonly List<Action<IDTasksAspNetCoreCoreConfigurationBuilder>> _configureAspNetCoreActions = [];

    IDependencyInjectionDTasksConfigurationBuilder IDTasksAspNetCoreCoreConfigurationBuilder.DTasks => dTasks;

    IDependencyInjectionDTasksConfigurationBuilder IDTasksAspNetCoreConfigurationBuilder.DTasks => dTasks;

    public TBuilder Configure<TBuilder>(TBuilder builder)
        where TBuilder : IDependencyInjectionDTasksConfigurationBuilder
    {
        return builder
            .AddAspNetCore(aspNetCore =>
            {
                foreach (var configure in _configureAspNetCoreActions)
                {
                    configure(aspNetCore);
                }
            })
            .UseSerialization(serialization =>
            {
                AspNetCoreSerializationConfigurationBuilder aspNetCoreSerialization = new(serialization);
                foreach (var configure in _configureSerializationActions)
                {
                    configure(aspNetCoreSerialization);
                }

                aspNetCoreSerialization.Configure();
            });
    }

    IDTasksAspNetCoreConfigurationBuilder IDTasksAspNetCoreConfigurationBuilder.RegisterEndpointResult<TResult>()
    {
        _configureAspNetCoreActions.Add(aspNetCore => aspNetCore.RegisterEndpointResult<TResult>());
        return this;
    }

    IDTasksAspNetCoreConfigurationBuilder IDTasksAspNetCoreConfigurationBuilder.AddResumptionEndpoint(ResumptionEndpoint endpoint)
    {
        _configureAspNetCoreActions.Add(aspNetCore => aspNetCore.AddResumptionEndpoint(endpoint));
        return this;
    }

    IDTasksAspNetCoreConfigurationBuilder IDTasksAspNetCoreConfigurationBuilder.AddResumptionEndpoint<TResult>(ResumptionEndpoint<TResult> endpoint)
    {
        _configureAspNetCoreActions.Add(aspNetCore => aspNetCore.AddResumptionEndpoint(endpoint));
        return this;
    }

    IDTasksAspNetCoreConfigurationBuilder IDTasksAspNetCoreConfigurationBuilder.AddDefaultResumptionEndpoint<TResult>()
    {
        _configureAspNetCoreActions.Add(aspNetCore => aspNetCore.AddDefaultResumptionEndpoint<TResult>());
        return this;
    }

    IDTasksAspNetCoreConfigurationBuilder IDTasksAspNetCoreConfigurationBuilder.ConfigureDTasksOptions(Action<OptionsBuilder<DTasksAspNetCoreOptions>> configure)
    {
        _configureAspNetCoreActions.Add(aspNetCore => aspNetCore.ConfigureDTasksOptions(configure));
        return this;
    }

    IDTasksAspNetCoreConfigurationBuilder IDTasksAspNetCoreConfigurationBuilder.UseDTasksOptions(DTasksAspNetCoreOptions options)
    {
        _configureAspNetCoreActions.Add(aspNetCore => aspNetCore.UseDTasksOptions(options));
        return this;
    }

    IDTasksAspNetCoreConfigurationBuilder IDTasksAspNetCoreConfigurationBuilder.ConfigureSerialization(Action<IAspNetCoreSerializationConfigurationBuilder> configure)
    {
        _configureSerializationActions.Add(configure);
        return this;
    }

    IDTasksAspNetCoreCoreConfigurationBuilder IDTasksAspNetCoreCoreConfigurationBuilder.RegisterEndpointResult<TResult>()
    {
        return ((IDTasksAspNetCoreConfigurationBuilder)this).RegisterEndpointResult<TResult>();
    }
    
    IDTasksAspNetCoreCoreConfigurationBuilder IDTasksAspNetCoreCoreConfigurationBuilder.AddResumptionEndpoint(ResumptionEndpoint endpoint)
    {
        return ((IDTasksAspNetCoreConfigurationBuilder)this).AddResumptionEndpoint(endpoint);
    }

    IDTasksAspNetCoreCoreConfigurationBuilder IDTasksAspNetCoreCoreConfigurationBuilder.AddResumptionEndpoint<TResult>(ResumptionEndpoint<TResult> endpoint)
    {
        return ((IDTasksAspNetCoreConfigurationBuilder)this).AddResumptionEndpoint(endpoint);
    }

    IDTasksAspNetCoreCoreConfigurationBuilder IDTasksAspNetCoreCoreConfigurationBuilder.AddDefaultResumptionEndpoint<TResult>()
    {
        return ((IDTasksAspNetCoreConfigurationBuilder)this).AddDefaultResumptionEndpoint<TResult>();
    }

    IDTasksAspNetCoreCoreConfigurationBuilder IDTasksAspNetCoreCoreConfigurationBuilder.UseDTasksOptions(DTasksAspNetCoreOptions options)
    {
        return ((IDTasksAspNetCoreConfigurationBuilder)this).UseDTasksOptions(options);
    }

    IDTasksAspNetCoreCoreConfigurationBuilder IDTasksAspNetCoreCoreConfigurationBuilder.ConfigureDTasksOptions(Action<OptionsBuilder<DTasksAspNetCoreOptions>> configure)
    {
        return ((IDTasksAspNetCoreConfigurationBuilder)this).ConfigureDTasksOptions(configure);
    }
}
