using DTasks.Extensions.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.Microsoft.Hosting;

public sealed class DTasksServiceProviderFactory(ServiceProviderOptions options) : IServiceProviderFactory<IServiceCollection>
{
    public IServiceCollection CreateBuilder(IServiceCollection services) => services;

    public IServiceProvider CreateServiceProvider(IServiceCollection services)
    {
        IServiceProvider provider = services
            .AddDTasks()
            .BuildServiceProvider(options);

        if (options.ValidateOnBuild)
        {
            var validator = provider.GetRequiredService<DAsyncServiceValidator>();
            validator();
        }

        return provider;
    }
}
