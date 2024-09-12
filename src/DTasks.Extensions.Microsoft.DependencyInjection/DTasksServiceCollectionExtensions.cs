using DTasks;
using DTasks.Extensions.Microsoft.DependencyInjection;
using DTasks.Extensions.Microsoft.DependencyInjection.Hosting;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.DependencyInjection;

public static class DTasksServiceCollectionExtensions
{
    public static IServiceCollection AddDTasks(this IServiceCollection services)
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(DTaskScope)))
            throw new InvalidOperationException("DTasks services have already been added.");

        ServiceContainerBuilder containerBuilder = ServiceContainerBuilder.Create(services);

        containerBuilder.ScanAndIntercept(services);
        containerBuilder.AddDTaskServices();

        return services;
    }
}
