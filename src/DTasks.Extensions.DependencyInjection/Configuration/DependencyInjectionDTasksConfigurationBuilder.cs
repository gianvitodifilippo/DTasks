using System.Collections.Frozen;
using System.Collections.Immutable;
using DTasks.Configuration;
using DTasks.Configuration.DependencyInjection;
using DTasks.Extensions.DependencyInjection.Infrastructure;
using DTasks.Extensions.DependencyInjection.Infrastructure.Marshaling;
using DTasks.Infrastructure;
using DTasks.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection.Configuration;

internal sealed class DependencyInjectionDTasksConfigurationBuilder(IServiceCollection services) : IDependencyInjectionDTasksConfigurationBuilder
{
    private readonly ServiceConfigurationBuilder _serviceConfigurationBuilder = new();
    private readonly List<Action<IDTasksConfigurationBuilder>> _configureActions = [];

    IServiceCollection IDependencyInjectionDTasksConfigurationBuilder.Services => services;

    public IServiceCollection Configure()
    {
        ImmutableArray<ServiceDescriptor> dAsyncDescriptors = [.. services.Where(_serviceConfigurationBuilder.IsDAsyncService)];
        FrozenSet<Type> dAsyncTypes = dAsyncDescriptors.Select(descriptor => descriptor.ServiceType).ToFrozenSet();

        DTasksConfiguration configuration = DTasksConfiguration.Build(builder =>
        {
            foreach (var configure in _configureActions)
            {
                configure(builder);
            }

            builder
                .ConfigureMarshaling(marshaling => marshaling
                    .AddSurrogator(InfrastructureServiceProvider.Descriptor.Map(provider => provider
                        .GetRequiredService<DAsyncSurrogatorProvider>()
                        .GetSurrogator(provider)))
                    .RegisterTypeId(typeof(ServiceSurrogate))
                    .RegisterTypeId(typeof(KeyedServiceSurrogate<string>))
                    .RegisterTypeId(typeof(KeyedServiceSurrogate<int>))
                    .RegisterTypeIds(dAsyncTypes));
        });

        ServiceContainerBuilder containerBuilder = new(services, configuration.Infrastructure.TypeResolver);
        foreach (ServiceDescriptor descriptor in dAsyncDescriptors)
        {
            containerBuilder.Replace(descriptor);
        }

        DAsyncServiceRegister serviceRegister = new(dAsyncTypes, configuration.Infrastructure.TypeResolver);
        DAsyncServiceValidator validator = containerBuilder.ValidationErrors.Count == 0
            ? () => { }
            : () => throw new AggregateException("Some d-async services are not able to be constructed.", containerBuilder.ValidationErrors);

        return services
            .AddSingleton<DAsyncHostInfrastructureProvider>()
            .AddSingleton(configuration)
            .AddSingleton(validator)
            .AddSingleton(configuration.Infrastructure.TypeResolver)
            .AddSingleton<IDAsyncServiceRegister>(serviceRegister)
            .AddSingleton<IServiceMapper, ServiceMapper>()
            .AddSingleton<DAsyncSurrogatorProvider>()
            .AddSingleton<RootDAsyncSurrogator>()
            .AddSingleton<IRootDAsyncSurrogator>(provider => provider.GetRequiredService<RootDAsyncSurrogator>())
            .AddSingleton<IRootServiceMapper>(provider => provider.GetRequiredService<RootDAsyncSurrogator>())
            .AddScoped<ChildDAsyncSurrogator>()
            .AddScoped<IChildServiceMapper>(provider => provider.GetRequiredService<ChildDAsyncSurrogator>());
    }

    IDependencyInjectionDTasksConfigurationBuilder IDependencyInjectionDTasksConfigurationBuilder.SetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, TProperty value)
    {
        _configureActions.Add(builder => builder.SetProperty(key, value));
        return this;
    }

    IDependencyInjectionDTasksConfigurationBuilder IDependencyInjectionDTasksConfigurationBuilder.ConfigureMarshaling(Action<IMarshalingConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        _configureActions.Add(builder => builder.ConfigureMarshaling(configure));
        return this;
    }

    IDependencyInjectionDTasksConfigurationBuilder IDependencyInjectionDTasksConfigurationBuilder.ConfigureState(Action<IStateConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        _configureActions.Add(builder => builder.ConfigureState(configure));
        return this;
    }

    IDependencyInjectionDTasksConfigurationBuilder IDependencyInjectionDTasksConfigurationBuilder.ConfigureExecution(Action<IExecutionConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        _configureActions.Add(builder => builder.ConfigureExecution(configure));
        return this;
    }

    IDependencyInjectionDTasksConfigurationBuilder IDependencyInjectionDTasksConfigurationBuilder.ConfigureServices(Action<IServiceConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        configure(_serviceConfigurationBuilder);
        return this;
    }

    IDTasksConfigurationBuilder IDTasksConfigurationBuilder.SetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, TProperty value)
    {
        return ((IDependencyInjectionDTasksConfigurationBuilder)this).SetProperty(key, value);
    }

    IDTasksConfigurationBuilder IDTasksConfigurationBuilder.ConfigureMarshaling(Action<IMarshalingConfigurationBuilder> configure)
    {
        return ((IDependencyInjectionDTasksConfigurationBuilder)this).ConfigureMarshaling(configure);
    }

    IDTasksConfigurationBuilder IDTasksConfigurationBuilder.ConfigureState(Action<IStateConfigurationBuilder> configure)
    {
        return ((IDependencyInjectionDTasksConfigurationBuilder)this).ConfigureState(configure);
    }

    IDTasksConfigurationBuilder IDTasksConfigurationBuilder.ConfigureExecution(Action<IExecutionConfigurationBuilder> configure)
    {
        return ((IDependencyInjectionDTasksConfigurationBuilder)this).ConfigureExecution(configure);
    }
}