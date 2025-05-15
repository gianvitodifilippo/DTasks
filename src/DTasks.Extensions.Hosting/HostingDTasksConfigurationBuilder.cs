using System.Diagnostics;
using DTasks.Configuration;
using DTasks.Extensions.DependencyInjection;
using DTasks.Extensions.DependencyInjection.Configuration;
using DTasks.Infrastructure;
using DTasks.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DTasks.Extensions.Hosting;

internal sealed class HostingDTasksConfigurationBuilder(HostBuilderContext context, IServiceCollection services) : IHostingDTasksConfigurationBuilder
{
    private readonly List<Action<IDependencyInjectionDTasksConfigurationBuilder>> _configureActions = [];
    private ServiceProviderOptions? _serviceProviderOptions;
    private Action<ServiceProviderOptions>? _configureServiceProviderOptions1;
    private Action<HostBuilderContext, ServiceProviderOptions>? _configureServiceProviderOptions2;

    IServiceCollection IDependencyInjectionDTasksConfigurationBuilder.Services => services;

    public IServiceProvider BuildServiceProvider()
    {
        services.AddDTasks(builder =>
        {
            foreach (var configure in _configureActions)
            {
                configure(builder);
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

    IHostingDTasksConfigurationBuilder IHostingDTasksConfigurationBuilder.SetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, TProperty value)
    {
        _configureActions.Add(builder => builder.SetProperty(key, value));
        return this;
    }

    IHostingDTasksConfigurationBuilder IHostingDTasksConfigurationBuilder.ConfigureMarshaling(Action<IMarshalingConfigurationBuilder> configure)
    {
        _configureActions.Add(builder => builder.ConfigureMarshaling(configure));
        return this;
    }

    IHostingDTasksConfigurationBuilder IHostingDTasksConfigurationBuilder.ConfigureState(Action<IStateConfigurationBuilder> configure)
    {
        _configureActions.Add(builder => builder.ConfigureState(configure));
        return this;
    }

    IHostingDTasksConfigurationBuilder IHostingDTasksConfigurationBuilder.ConfigureExecution(Action<IExecutionConfigurationBuilder> configure)
    {
        _configureActions.Add(builder => builder.ConfigureExecution(configure));
        return this;
    }

    IHostingDTasksConfigurationBuilder IHostingDTasksConfigurationBuilder.ConfigureServices(Action<IServiceConfigurationBuilder> configure)
    {
        _configureActions.Add(builder => builder.ConfigureServices(configure));
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

    IDependencyInjectionDTasksConfigurationBuilder IDependencyInjectionDTasksConfigurationBuilder.SetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, TProperty value)
    {
        return ((IHostingDTasksConfigurationBuilder)this).SetProperty(key, value);
    }

    IDependencyInjectionDTasksConfigurationBuilder IDependencyInjectionDTasksConfigurationBuilder.ConfigureMarshaling(Action<IMarshalingConfigurationBuilder> configure)
    {
        return ((IHostingDTasksConfigurationBuilder)this).ConfigureMarshaling(configure);
    }

    IDependencyInjectionDTasksConfigurationBuilder IDependencyInjectionDTasksConfigurationBuilder.ConfigureState(Action<IStateConfigurationBuilder> configure)
    {
        return ((IHostingDTasksConfigurationBuilder)this).ConfigureState(configure);
    }

    IDependencyInjectionDTasksConfigurationBuilder IDependencyInjectionDTasksConfigurationBuilder.ConfigureExecution(Action<IExecutionConfigurationBuilder> configure)
    {
        return ((IHostingDTasksConfigurationBuilder)this).ConfigureExecution(configure);
    }

    IDependencyInjectionDTasksConfigurationBuilder IDependencyInjectionDTasksConfigurationBuilder.ConfigureServices(Action<IServiceConfigurationBuilder> configure)
    {
        return ((IHostingDTasksConfigurationBuilder)this).ConfigureServices(configure);
    }

    IDTasksConfigurationBuilder IDTasksConfigurationBuilder.SetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, TProperty value)
    {
        return ((IHostingDTasksConfigurationBuilder)this).SetProperty(key, value);
    }

    IDTasksConfigurationBuilder IDTasksConfigurationBuilder.ConfigureMarshaling(Action<IMarshalingConfigurationBuilder> configure)
    {
        return ((IHostingDTasksConfigurationBuilder)this).ConfigureMarshaling(configure);
    }

    IDTasksConfigurationBuilder IDTasksConfigurationBuilder.ConfigureState(Action<IStateConfigurationBuilder> configure)
    {
        return ((IHostingDTasksConfigurationBuilder)this).ConfigureState(configure);
    }

    IDTasksConfigurationBuilder IDTasksConfigurationBuilder.ConfigureExecution(Action<IExecutionConfigurationBuilder> configure)
    {
        return ((IHostingDTasksConfigurationBuilder)this).ConfigureExecution(configure);
    }
}
