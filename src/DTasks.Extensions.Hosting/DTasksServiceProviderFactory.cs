using DTasks.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.Hosting;

public sealed class DTasksServiceProviderFactory(ServiceProviderOptions options) : IServiceProviderFactory<IServiceCollection>
{
    public IServiceCollection CreateBuilder(IServiceCollection services) => services;

    public IServiceProvider CreateServiceProvider(IServiceCollection services)
    {
        ServiceProvider provider = services
            .AddDTasks()
            .BuildServiceProvider(options);

        if (options.ValidateOnBuild)
        {
            var validateDAsyncServices = provider.GetRequiredService<DAsyncServiceValidator>();
            validateDAsyncServices();
        }

        return provider;
    }
}
