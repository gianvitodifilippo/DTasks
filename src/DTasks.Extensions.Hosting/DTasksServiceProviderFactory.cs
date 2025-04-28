using DTasks.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DTasks.Extensions.Hosting;

public sealed class DTasksServiceProviderFactory(
    HostBuilderContext context,
    Action<IHostingDTasksConfigurationBuilder> configure) : IServiceProviderFactory<IServiceCollection>
{
    public IServiceCollection CreateBuilder(IServiceCollection services) => services;

    public IServiceProvider CreateServiceProvider(IServiceCollection services)
    {
        HostingDTasksConfigurationBuilder builder = new(context, services);
        configure(builder);

        return builder.BuildServiceProvider();
    }
}
