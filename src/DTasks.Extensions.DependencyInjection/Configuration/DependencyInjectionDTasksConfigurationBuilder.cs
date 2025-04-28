using System.Collections.Frozen;
using System.Collections.Immutable;
using DTasks.Configuration;
using DTasks.Configuration.DependencyInjection;
using DTasks.Extensions.DependencyInjection.Infrastructure.Marshaling;
using DTasks.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection.Configuration;

internal sealed class DependencyInjectionDTasksConfigurationBuilder(IServiceCollection services) : IDependencyInjectionDTasksConfigurationBuilder
{
    private readonly ServiceConfigurationBuilder _serviceConfigurationBuilder = new();
    private readonly List<Action<IMarshalingConfigurationBuilder>> _configureMarshalingActions = [];
    private readonly List<Action<IStateConfigurationBuilder>> _configureStateActions = [];
    private readonly List<Action<IExecutionConfigurationBuilder>> _configureExecutionActions = [];

    IServiceCollection IDependencyInjectionDTasksConfigurationBuilder.Services => services;

    public IServiceCollection Configure()
    {
        ImmutableArray<ServiceDescriptor> dAsyncDescriptors = [.. services.Where(_serviceConfigurationBuilder.IsDAsyncService)];
        FrozenSet<Type> dAsyncTypes = dAsyncDescriptors.Select(descriptor => descriptor.ServiceType).ToFrozenSet();

        DTasksConfiguration configuration = DTasksConfiguration.Create(builder =>
        {
            foreach (var configure in _configureMarshalingActions)
            {
                builder.ConfigureMarshaling(configure);
            }

            foreach (var configure in _configureStateActions)
            {
                builder.ConfigureState(configure);
            }

            foreach (var configure in _configureExecutionActions)
            {
                builder.ConfigureExecution(configure);
            }

            builder
                .ConfigureMarshaling(marshaling => marshaling
                    .AddSurrogator(InfrastructureServiceProvider.Descriptor.Map(provider => provider.GetRequiredService<DAsyncSurrogatorProvider>().GetSurrogator(provider)))
                    .RegisterTypeId(typeof(ServiceSurrogate))
                    .RegisterTypeId(typeof(KeyedServiceSurrogate<string>))
                    .RegisterTypeId(typeof(KeyedServiceSurrogate<int>))
                    .RegisterTypeIds(dAsyncTypes));
        });

        ServiceContainerBuilder containerBuilder = new(services, configuration.TypeResolver);
        foreach (ServiceDescriptor descriptor in dAsyncDescriptors)
        {
            containerBuilder.Replace(descriptor);
        }

        DAsyncServiceRegister serviceRegister = new(dAsyncTypes, configuration.TypeResolver);
        DAsyncServiceValidator validator = containerBuilder.ValidationErrors.Count == 0
            ? () => { }
            : () => throw new AggregateException("Some d-async services are not able to be constructed.", containerBuilder.ValidationErrors);

        return services
            .AddSingleton(configuration)
            .AddSingleton(validator)
            .AddSingleton(configuration.TypeResolver)
            .AddSingleton<IDAsyncServiceRegister>(serviceRegister)
            .AddSingleton<IServiceMapper, ServiceMapper>()
            .AddSingleton<DAsyncSurrogatorProvider>()
            .AddSingleton<RootDAsyncSurrogator>()
            .AddSingleton<IRootDAsyncSurrogator>(provider => provider.GetRequiredService<RootDAsyncSurrogator>())
            .AddSingleton<IRootServiceMapper>(provider => provider.GetRequiredService<RootDAsyncSurrogator>())
            .AddScoped<ChildDAsyncSurrogator>()
            .AddScoped<IChildServiceMapper>(provider => provider.GetRequiredService<ChildDAsyncSurrogator>());
    }

    IDependencyInjectionDTasksConfigurationBuilder IDependencyInjectionDTasksConfigurationBuilder.ConfigureMarshaling(Action<IMarshalingConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        _configureMarshalingActions.Add(configure);
        return this;
    }

    IDependencyInjectionDTasksConfigurationBuilder IDependencyInjectionDTasksConfigurationBuilder.ConfigureState(Action<IStateConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        _configureStateActions.Add(configure);
        return this;
    }

    IDependencyInjectionDTasksConfigurationBuilder IDependencyInjectionDTasksConfigurationBuilder.ConfigureExecution(Action<IExecutionConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        _configureExecutionActions.Add(configure);
        return this;
    }

    IDependencyInjectionDTasksConfigurationBuilder IDependencyInjectionDTasksConfigurationBuilder.ConfigureServices(Action<IServiceConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        configure(_serviceConfigurationBuilder);
        return this;
    }

    IDTasksConfigurationBuilder IDTasksConfigurationBuilder.ConfigureMarshaling(Action<IMarshalingConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        _configureMarshalingActions.Add(configure);
        return this;
    }

    IDTasksConfigurationBuilder IDTasksConfigurationBuilder.ConfigureState(Action<IStateConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        _configureStateActions.Add(configure);
        return this;
    }

    IDTasksConfigurationBuilder IDTasksConfigurationBuilder.ConfigureExecution(Action<IExecutionConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        _configureExecutionActions.Add(configure);
        return this;
    }
}