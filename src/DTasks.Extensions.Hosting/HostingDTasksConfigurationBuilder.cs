using System.Diagnostics;
using DTasks.Configuration;
using DTasks.Extensions.DependencyInjection;
using DTasks.Extensions.DependencyInjection.Configuration;
using DTasks.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DTasks.Extensions.Hosting;

internal sealed class HostingDTasksConfigurationBuilder(HostBuilderContext context, IServiceCollection services) : IHostingDTasksConfigurationBuilder
{
    private readonly List<Action<IServiceConfigurationBuilder>> _configureServicesActions = [];
    private readonly List<Action<IMarshalingConfigurationBuilder>> _configureMarshalingActions = [];
    private readonly List<Action<IStateConfigurationBuilder>> _configureStateActions = [];
    private readonly List<Action<IExecutionConfigurationBuilder>> _configureExecutionActions = [];
    private ServiceProviderOptions? _serviceProviderOptions;
    private Action<ServiceProviderOptions>? _configureServiceProviderOptions1;
    private Action<HostBuilderContext, ServiceProviderOptions>? _configureServiceProviderOptions2;
    private object? _serviceProviderOptionsOrConfiguration;

    IServiceCollection IDependencyInjectionDTasksConfigurationBuilder.Services => services;

    public IServiceProvider BuildServiceProvider()
    {
        services.AddDTasks(builder =>
        {
            foreach (var action in _configureServicesActions)
            {
                builder.ConfigureServices(action);
            }

            foreach (var action in _configureMarshalingActions)
            {
                builder.ConfigureMarshaling(action);
            }
            
            foreach (var action in _configureStateActions)
            {
                builder.ConfigureState(action);
            }
            
            foreach (var action in _configureExecutionActions)
            {
                builder.ConfigureExecution(action);
            }
        });

        ServiceProvider provider = services.BuildServiceProvider();
        ServiceProviderOptions options = GetServiceProviderOptions();

        if (options.ValidateOnBuild)
        {
            var validateDAsyncServices = provider.GetRequiredService<DAsyncServiceValidator>();
            validateDAsyncServices();
        }

        return provider;
    }

    private ServiceProviderOptions GetServiceProviderOptions()
    {
        if (_serviceProviderOptionsOrConfiguration is null)
        {
            Debug.Assert(_serviceProviderOptions is null && _configureServiceProviderOptions1 is null && _configureServiceProviderOptions2 is null);
            return new ServiceProviderOptions();
        }

        if (ReferenceEquals(_serviceProviderOptionsOrConfiguration, _serviceProviderOptions))
            return _serviceProviderOptions;

        ServiceProviderOptions options = new();
        if (ReferenceEquals(_serviceProviderOptionsOrConfiguration, _configureServiceProviderOptions1))
        {
            _configureServiceProviderOptions1(options);
        }
        else if (ReferenceEquals(_serviceProviderOptionsOrConfiguration, _configureServiceProviderOptions2))
        {
            _configureServiceProviderOptions2(context, options);
        }

        return options;
    }

    IDependencyInjectionDTasksConfigurationBuilder IHostingDTasksConfigurationBuilder.ConfigureMarshaling(Action<IMarshalingConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        _configureMarshalingActions.Add(configure);
        return this;
    }

    IDependencyInjectionDTasksConfigurationBuilder IHostingDTasksConfigurationBuilder.ConfigureState(Action<IStateConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        _configureStateActions.Add(configure);
        return this;
    }

    IDependencyInjectionDTasksConfigurationBuilder IHostingDTasksConfigurationBuilder.ConfigureExecution(Action<IExecutionConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        _configureExecutionActions.Add(configure);
        return this;
    }

    IDependencyInjectionDTasksConfigurationBuilder IHostingDTasksConfigurationBuilder.ConfigureServices(Action<IServiceConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        _configureServicesActions.Add(configure);
        return this;
    }

    IHostingDTasksConfigurationBuilder IHostingDTasksConfigurationBuilder.UseServiceProviderOptions(ServiceProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        _serviceProviderOptions = options;
        _serviceProviderOptionsOrConfiguration = options;
        return this;
    }

    IHostingDTasksConfigurationBuilder IHostingDTasksConfigurationBuilder.UseServiceProviderOptions(Action<ServiceProviderOptions> configureOptions)
    {
        ThrowHelper.ThrowIfNull(configureOptions);

        _configureServiceProviderOptions1 = configureOptions;
        _serviceProviderOptionsOrConfiguration = configureOptions;
        return this;
    }

    IHostingDTasksConfigurationBuilder IHostingDTasksConfigurationBuilder.UseServiceProviderOptions(Action<HostBuilderContext, ServiceProviderOptions> configureOptions)
    {
        ThrowHelper.ThrowIfNull(configureOptions);

        _configureServiceProviderOptions2 = configureOptions;
        _serviceProviderOptionsOrConfiguration = configureOptions;
        return this;
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

        _configureServicesActions.Add(configure);
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
