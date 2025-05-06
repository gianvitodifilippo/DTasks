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
        if (_serviceProviderOptions is not null)
        {
            Debug.Assert(_configureServiceProviderOptions1 is null && _configureServiceProviderOptions2 is null);
            return _serviceProviderOptions;
        }

        ServiceProviderOptions options = new();
        if (_configureServiceProviderOptions1 is not null)
        {
            Debug.Assert(_configureServiceProviderOptions2 is null);
            
            _configureServiceProviderOptions1(options);
            return options;
        }
        
        if (_configureServiceProviderOptions2 is not null)
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
        _configureServiceProviderOptions1 = null;
        _configureServiceProviderOptions2 = null;
        return this;
    }

    IHostingDTasksConfigurationBuilder IHostingDTasksConfigurationBuilder.UseServiceProviderOptions(Action<ServiceProviderOptions> configureOptions)
    {
        ThrowHelper.ThrowIfNull(configureOptions);

        _serviceProviderOptions = null;
        _configureServiceProviderOptions1 = configureOptions;
        _configureServiceProviderOptions2 = null;
        return this;
    }

    IHostingDTasksConfigurationBuilder IHostingDTasksConfigurationBuilder.UseServiceProviderOptions(Action<HostBuilderContext, ServiceProviderOptions> configureOptions)
    {
        ThrowHelper.ThrowIfNull(configureOptions);

        _serviceProviderOptions = null;
        _configureServiceProviderOptions1 = null;
        _configureServiceProviderOptions2 = configureOptions;
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
