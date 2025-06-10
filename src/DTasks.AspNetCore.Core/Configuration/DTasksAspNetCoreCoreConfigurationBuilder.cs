using DTasks.AspNetCore.Execution;
using DTasks.AspNetCore.Infrastructure.Execution;
using DTasks.AspNetCore.Infrastructure.Http;
using DTasks.Configuration;
using DTasks.Extensions.DependencyInjection.Configuration;
using DTasks.Infrastructure.Generics;
using DTasks.Infrastructure.Marshaling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DTasks.AspNetCore.Configuration;

internal sealed class DTasksAspNetCoreCoreConfigurationBuilder(IDependencyInjectionDTasksConfigurationBuilder dTasks) : IDTasksAspNetCoreCoreConfigurationBuilder
{
    private readonly WebSuspensionRegisterBuilder _suspensionRegisterBuilder = new();
    private readonly List<Action<OptionsBuilder<DTasksAspNetCoreOptions>>> _configureOptionsActions = [];
    private readonly HashSet<ITypeContext> _endpointResultTypeContexts = [];
    private DTasksAspNetCoreOptions? _customOptions;

    IDependencyInjectionDTasksConfigurationBuilder IDTasksAspNetCoreCoreConfigurationBuilder.DTasks => dTasks;

    public TBuilder Configure<TBuilder>(TBuilder builder)
        where TBuilder : IDependencyInjectionDTasksConfigurationBuilder
    {
        builder.Services
            .AddHttpClient()
            .AddSingleton<IDAsyncContinuationFactory, DAsyncContinuationFactory>()
            .AddSingleton(sp => _suspensionRegisterBuilder.Build(sp.GetRequiredService<DTasksConfiguration>().Infrastructure.TypeResolver))
            .AddHostedService(sp => sp.GetRequiredService<PollingDAsyncSuspensionHandler>())
            .AddSingleton<PollingDAsyncSuspensionHandler>(sp =>
            {
                IReminderStore? store = sp.GetService<IReminderStore>();
                return store is null
                    ? new HeapDAsyncSuspensionHandler(sp)
                    : new StoreDAsyncSuspensionHandler(sp, store);
            });

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
            .ConfigureExecution(execution => execution
                .UseSuspensionHandler(InfrastructureServiceProvider.GetRequiredService<PollingDAsyncSuspensionHandler>()))
            .ConfigureMarshaling(marshaling =>
            {
                RegisterAsyncEndpointInfoTypeIdAction registerEndpointResultsAction = new(marshaling);
                foreach (ITypeContext endpointResultTypeContext in _endpointResultTypeContexts)
                {
                    endpointResultTypeContext.Execute(ref registerEndpointResultsAction);
                }
                
                marshaling
                    .RegisterTypeId(TypeContext.Of<WebhookDAsyncContinuation.Surrogate>(), TypeId.FromEncodedTypeName(typeof(WebhookDAsyncContinuation.Surrogate)))
                    .RegisterTypeId(TypeContext.Of<WebSocketsDAsyncContinuation.Surrogate>(), TypeId.FromEncodedTypeName(typeof(WebSocketsDAsyncContinuation.Surrogate)));
            });

        return builder;
    }

    IDTasksAspNetCoreCoreConfigurationBuilder IDTasksAspNetCoreCoreConfigurationBuilder.RegisterEndpointResult<TResult>()
    {
        _endpointResultTypeContexts.Add(TypeContext.Of<TResult>());
        return this;
    }

    IDTasksAspNetCoreCoreConfigurationBuilder IDTasksAspNetCoreCoreConfigurationBuilder.AddResumptionEndpoint(ResumptionEndpoint endpoint)
    {
        _suspensionRegisterBuilder.AddResumptionEndpoint(endpoint);
        return this;
    }

    IDTasksAspNetCoreCoreConfigurationBuilder IDTasksAspNetCoreCoreConfigurationBuilder.AddResumptionEndpoint<TResult>(ResumptionEndpoint<TResult> endpoint)
    {
        _suspensionRegisterBuilder.AddResumptionEndpoint(endpoint);
        return this;
    }

    IDTasksAspNetCoreCoreConfigurationBuilder IDTasksAspNetCoreCoreConfigurationBuilder.AddDefaultResumptionEndpoint<TResult>()
    {
        _suspensionRegisterBuilder.AddDefaultResumptionEndpoint<TResult>();
        return this;
    }

    IDTasksAspNetCoreCoreConfigurationBuilder IDTasksAspNetCoreCoreConfigurationBuilder.UseDTasksOptions(DTasksAspNetCoreOptions options)
    {
        _customOptions = options;
        return this;
    }

    IDTasksAspNetCoreCoreConfigurationBuilder IDTasksAspNetCoreCoreConfigurationBuilder.ConfigureDTasksOptions(Action<OptionsBuilder<DTasksAspNetCoreOptions>> configure)
    {
        _configureOptionsActions.Add(configure);
        return this;
    }
    
    private readonly struct RegisterAsyncEndpointInfoTypeIdAction(IMarshalingConfigurationBuilder marshaling) : ITypeAction
    {
        public void Invoke<T>()
        {
            marshaling.RegisterTypeId(TypeContext.Of<AsyncEndpointInfo<T>>(), TypeId.FromEncodedTypeName(typeof(AsyncEndpointInfo<T>)));
        }
    }
}