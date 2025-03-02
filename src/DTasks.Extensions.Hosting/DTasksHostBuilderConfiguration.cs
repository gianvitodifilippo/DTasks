using DTasks.Extensions.DependencyInjection;
using DTasks.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;

namespace DTasks.Extensions.Hosting;

internal sealed class DTasksHostBuilderConfiguration(HostBuilderContext context, IServiceCollection services) : IDTasksHostBuilderConfiguration
{
    private ServiceProviderOptions? _serviceProviderOptions;
    private Action<ServiceProviderOptions>? _configureServiceProviderOptions1;
    private Action<HostBuilderContext, ServiceProviderOptions>? _configureServiceProviderOptions2;
    private object? _serviceProviderOptionsOrConfiguration;
    private Action<IDTasksServiceConfiguration>? _configureDTasksServices;

    public IServiceProvider BuildServiceProvider()
    {
        if (_configureDTasksServices is not null)
        {
            services.AddDTasks(_configureDTasksServices);
        }
        else
        {
            services.AddDTasks();
        }

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

    public IDTasksHostBuilderConfiguration UseServiceProviderOptions(ServiceProviderOptions options)
    {
        ThrowHelper.ThrowIfNull(options);

        _serviceProviderOptions = options;
        _serviceProviderOptionsOrConfiguration = options;
        return this;
    }

    public IDTasksHostBuilderConfiguration UseServiceProviderOptions(Action<ServiceProviderOptions> configureOptions)
    {
        ThrowHelper.ThrowIfNull(configureOptions);

        _configureServiceProviderOptions1 = configureOptions;
        _serviceProviderOptionsOrConfiguration = configureOptions;
        return this;
    }

    public IDTasksHostBuilderConfiguration UseServiceProviderOptions(Action<HostBuilderContext, ServiceProviderOptions> configureOptions)
    {
        ThrowHelper.ThrowIfNull(configureOptions);

        _configureServiceProviderOptions2 = configureOptions;
        _serviceProviderOptionsOrConfiguration = configureOptions;
        return this;
    }

    public IDTasksHostBuilderConfiguration ConfigureDTasks(Action<IDTasksServiceConfiguration> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        _configureDTasksServices = configure;
        return this;
    }
}
