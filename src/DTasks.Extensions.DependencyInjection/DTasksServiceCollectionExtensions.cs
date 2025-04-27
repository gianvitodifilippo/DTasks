using DTasks.Configuration;
using DTasks.Extensions.DependencyInjection.Configuration;
using DTasks.Utils;

namespace Microsoft.Extensions.DependencyInjection;

public static class DTasksServiceCollectionExtensions
{
    public static IServiceCollection AddDTasks(this IServiceCollection services, Action<IDependencyInjectionDTasksConfigurationBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(configure);

        if (services.Any(descriptor => descriptor.ServiceType == typeof(DTasksServiceMarker)))
            throw new InvalidOperationException($"DTasks services have already been added. Make sure to call '{nameof(AddDTasks)}' once and after registering all other services.");

        services.AddSingleton<DTasksServiceMarker>();

        DependencyInjectionDTasksConfigurationBuilder builder = new(services);
        configure(builder);

        return builder.Configure();
    }

    private sealed class DTasksServiceMarker;
}
