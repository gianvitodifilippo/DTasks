﻿using DTasks.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DTasks.Extensions.Hosting;

public interface IDTasksHostBuilderConfiguration
{
    IServiceCollection Services { get; }

    IDTasksHostBuilderConfiguration UseServiceProviderOptions(ServiceProviderOptions options);

    IDTasksHostBuilderConfiguration UseServiceProviderOptions(Action<ServiceProviderOptions> configureOptions);

    IDTasksHostBuilderConfiguration UseServiceProviderOptions(Action<HostBuilderContext, ServiceProviderOptions> configureOptions);

    IDTasksHostBuilderConfiguration Configure(Action<IDependencyInjectionDTasksConfigurationBuilder> configure);
}
