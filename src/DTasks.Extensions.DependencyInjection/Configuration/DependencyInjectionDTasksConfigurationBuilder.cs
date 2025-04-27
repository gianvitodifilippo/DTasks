using DTasks.Configuration;
using DTasks.Infrastructure.Marshaling;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection.Configuration;

internal sealed class DependencyInjectionDTasksConfigurationBuilder : IDependencyInjectionDTasksConfigurationBuilder
{
    private readonly ServiceConfigurationBuilder _serviceConfiguration = new();
    private readonly List<Action<IMarshalingConfigurationBuilder>> _configureMarshalingActions = [];
    private readonly List<Action<IStateConfigurationBuilder>> _configureStateActions = [];
    private readonly List<Action<IExecutionConfigurationBuilder>> _configureExecutionActions = [];

    public IServiceCollection Configure(IServiceCollection services)
    {
        return services.AddSingleton(sp => DTasksConfiguration.Create(builder =>
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
                    .AddSurrogator(InfrastructureServiceProvider.GetRequiredService<IDAsyncSurrogator>()));
        }));
    }

    IDependencyInjectionDTasksConfigurationBuilder IDTasksConfigurationBuilder<IDependencyInjectionDTasksConfigurationBuilder>.ConfigureMarshaling(Action<IMarshalingConfigurationBuilder> configure)
    {
        _configureMarshalingActions.Add(configure);
        return this;
    }

    IDependencyInjectionDTasksConfigurationBuilder IDTasksConfigurationBuilder<IDependencyInjectionDTasksConfigurationBuilder>.ConfigureState(Action<IStateConfigurationBuilder> configure)
    {
        _configureStateActions.Add(configure);
        return this;
    }

    IDependencyInjectionDTasksConfigurationBuilder IDTasksConfigurationBuilder<IDependencyInjectionDTasksConfigurationBuilder>.ConfigureExecution(Action<IExecutionConfigurationBuilder> configure)
    {
        _configureExecutionActions.Add(configure);
        return this;
    }

    IDependencyInjectionDTasksConfigurationBuilder IDependencyInjectionDTasksConfigurationBuilder<IDependencyInjectionDTasksConfigurationBuilder>.ConfigureServices(Action<IServiceConfigurationBuilder> configure)
    {
        configure(_serviceConfiguration);
        return this;
    }
}