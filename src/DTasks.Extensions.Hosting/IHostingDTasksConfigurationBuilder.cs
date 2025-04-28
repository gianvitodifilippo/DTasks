using DTasks.Extensions.DependencyInjection.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DTasks.Configuration;

public interface IHostingDTasksConfigurationBuilder : IDependencyInjectionDTasksConfigurationBuilder
{
    new IDependencyInjectionDTasksConfigurationBuilder ConfigureMarshaling(Action<IMarshalingConfigurationBuilder> configure);

    new IDependencyInjectionDTasksConfigurationBuilder ConfigureState(Action<IStateConfigurationBuilder> configure);

    new IDependencyInjectionDTasksConfigurationBuilder ConfigureExecution(Action<IExecutionConfigurationBuilder> configure);

    new IDependencyInjectionDTasksConfigurationBuilder ConfigureServices(Action<IServiceConfigurationBuilder> configure);

    IHostingDTasksConfigurationBuilder UseServiceProviderOptions(ServiceProviderOptions options);

    IHostingDTasksConfigurationBuilder UseServiceProviderOptions(Action<ServiceProviderOptions> configureOptions);

    IHostingDTasksConfigurationBuilder UseServiceProviderOptions(Action<HostBuilderContext, ServiceProviderOptions> configureOptions);
}
