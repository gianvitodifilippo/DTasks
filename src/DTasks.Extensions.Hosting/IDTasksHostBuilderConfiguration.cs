using DTasks.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DTasks.Extensions.Hosting;

public interface IDTasksHostBuilderConfiguration
{
    IDTasksHostBuilderConfiguration UseServiceProviderOptions(ServiceProviderOptions options);

    IDTasksHostBuilderConfiguration UseServiceProviderOptions(Action<ServiceProviderOptions> configureOptions);

    IDTasksHostBuilderConfiguration UseServiceProviderOptions(Action<HostBuilderContext, ServiceProviderOptions> configureOptions);

    IDTasksHostBuilderConfiguration ConfigureDTasks(Action<IDTasksServiceConfiguration> configure);
}
