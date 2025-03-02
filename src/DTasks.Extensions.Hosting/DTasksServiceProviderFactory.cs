using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DTasks.Extensions.Hosting;

public sealed class DTasksServiceProviderFactory(
    HostBuilderContext context,
    Action<IDTasksHostBuilderConfiguration> configure) : IServiceProviderFactory<IServiceCollection>
{
    public IServiceCollection CreateBuilder(IServiceCollection services) => services;

    public IServiceProvider CreateServiceProvider(IServiceCollection services)
    {
        DTasksHostBuilderConfiguration configuration = new(context, services);
        configure(configuration);

        return configuration.BuildServiceProvider();
    }
}
