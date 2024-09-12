using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.Microsoft.Hosting;

public sealed class DTasksServiceProviderFactory(ServiceProviderOptions options) : IServiceProviderFactory<IServiceCollection>
{
    public IServiceCollection CreateBuilder(IServiceCollection services) => services;

    public IServiceProvider CreateServiceProvider(IServiceCollection services)
    {
        return services
            .AddDTasks()
            .BuildServiceProvider(options);
    }
}
