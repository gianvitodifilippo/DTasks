using DTasks.Extensions.Microsoft.DependencyInjection;
using DTasks.Utils;

namespace Microsoft.Extensions.DependencyInjection;

public static class DTasksServiceCollectionExtensions
{
    public static IServiceCollection AddDTasks(this IServiceCollection services)
    {
        ThrowHelper.ThrowIfNull(services);

        return services.AddDTasksCore(config => { });
    }

    public static IServiceCollection AddDTasks(this IServiceCollection services, Action<IDTasksServiceConfiguration> configure)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(configure);
        
        return services.AddDTasksCore(configure);
    }
    
    private static IServiceCollection AddDTasksCore(this IServiceCollection services, Action<IDTasksServiceConfiguration> configure)
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(DTasksServiceMarker)))
            throw new InvalidOperationException($"DTasks services have already been added. Make sure to call '{nameof(AddDTasks)}' once after registering all services involved in d-async flows.");

        services.AddSingleton<DTasksServiceMarker>();

        DTasksServiceConfiguration configuration = new(services);
        configure(configuration);

        ServiceContainerBuilder containerBuilder = ServiceContainerBuilder.Create(services);
        configuration.ReplaceDAsyncServices(containerBuilder);
        containerBuilder.AddDTaskServices();

        return services;
    }

    private sealed class DTasksServiceMarker;
}
