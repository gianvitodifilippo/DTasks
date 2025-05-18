using DTasks.Extensions.DependencyInjection.Configuration;
using DTasks.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DTasks.Configuration;

public interface IHostingDTasksConfigurationBuilder : IDependencyInjectionDTasksConfigurationBuilder
{
    new IHostingDTasksConfigurationBuilder SetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, TProperty value);

    new IHostingDTasksConfigurationBuilder ConfigureMarshaling(Action<IMarshalingConfigurationBuilder> configure);

    new IHostingDTasksConfigurationBuilder ConfigureState(Action<IStateConfigurationBuilder> configure);

    new IHostingDTasksConfigurationBuilder ConfigureExecution(Action<IExecutionConfigurationBuilder> configure);

    new IHostingDTasksConfigurationBuilder ConfigureServices(Action<IServiceConfigurationBuilder> configure);

    IHostingDTasksConfigurationBuilder UseServiceProviderOptions(ServiceProviderOptions options);

    IHostingDTasksConfigurationBuilder UseServiceProviderOptions(Action<ServiceProviderOptions> configureOptions);

    IHostingDTasksConfigurationBuilder UseServiceProviderOptions(Action<HostBuilderContext, ServiceProviderOptions> configureOptions);
}
