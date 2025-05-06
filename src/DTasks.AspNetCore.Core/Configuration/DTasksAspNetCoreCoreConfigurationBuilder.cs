using DTasks.AspNetCore.Execution;
using DTasks.AspNetCore.Infrastructure.Http;
using DTasks.AspNetCore.State;
using DTasks.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DTasks.AspNetCore.Configuration;

internal sealed class DTasksAspNetCoreCoreConfigurationBuilder : IDTasksAspNetCoreCoreConfigurationBuilder
{
    private readonly WebSuspensionRegisterBuilder _suspensionRegisterBuilder = new();
    private readonly List<Action<OptionsBuilder<DTasksAspNetCoreOptions>>> _configureOptionsActions = [];
    private DTasksAspNetCoreOptions? _customOptions;

    public TBuilder Configure<TBuilder>(TBuilder builder)
        where TBuilder : IDependencyInjectionDTasksConfigurationBuilder
    {
        builder.Services
            .AddSingleton<IDAsyncContinuationFactory, DAsyncContinuationFactory>()
            .AddSingleton<EndpointStatusMonitor>()
            .AddSingleton(sp => _suspensionRegisterBuilder.Build(sp.GetRequiredService<DTasksConfiguration>().TypeResolver));

        if (_customOptions is not null)
        {
            builder.Services.AddSingleton(Options.Create(_customOptions));
        }
        else
        {
            OptionsBuilder<DTasksAspNetCoreOptions> optionsBuilder = builder.Services.AddOptions<DTasksAspNetCoreOptions>();
            foreach (var configureOptionsAction in _configureOptionsActions)
            {
                configureOptionsAction(optionsBuilder);
            }
        }

        builder
            .ConfigureMarshaling(marshaling => marshaling
                .RegisterTypeId(typeof(WebhookDAsyncContinuation.Surrogate))
                .RegisterTypeId(typeof(WebSocketsDAsyncContinuation.Surrogate)));

        return builder;
    }

    public IDTasksAspNetCoreCoreConfigurationBuilder AddResumptionEndpoint(ResumptionEndpoint endpoint)
    {
        _suspensionRegisterBuilder.AddResumptionEndpoint(endpoint);
        return this;
    }

    public IDTasksAspNetCoreCoreConfigurationBuilder AddResumptionEndpoint<TResult>(ResumptionEndpoint<TResult> endpoint)
    {
        _suspensionRegisterBuilder.AddResumptionEndpoint(endpoint);
        return this;
    }

    public IDTasksAspNetCoreCoreConfigurationBuilder AddDefaultResumptionEndpoint<TResult>()
    {
        _suspensionRegisterBuilder.AddDefaultResumptionEndpoint<TResult>();
        return this;
    }

    public IDTasksAspNetCoreCoreConfigurationBuilder UseDTasksOptions(DTasksAspNetCoreOptions options)
    {
        _customOptions = options;
        return this;
    }

    public IDTasksAspNetCoreCoreConfigurationBuilder ConfigureDTasksOptions(Action<OptionsBuilder<DTasksAspNetCoreOptions>> configure)
    {
        _configureOptionsActions.Add(configure);
        return this;
    }
}