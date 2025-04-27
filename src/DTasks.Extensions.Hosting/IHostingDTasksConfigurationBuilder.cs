using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DTasks.Configuration;

public interface IHostingDTasksConfigurationBuilder : IHostingDTasksConfigurationBuilder<IHostingDTasksConfigurationBuilder>;

public interface IHostingDTasksConfigurationBuilder<out TBuilder> : IDependencyInjectionDTasksConfigurationBuilder<TBuilder>
    where TBuilder : IHostingDTasksConfigurationBuilder<TBuilder>
{
    TBuilder UseServiceProviderOptions(ServiceProviderOptions options);

    TBuilder UseServiceProviderOptions(Action<ServiceProviderOptions> configureOptions);

    TBuilder UseServiceProviderOptions(Action<HostBuilderContext, ServiceProviderOptions> configureOptions);
}
