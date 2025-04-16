using DTasks.Infrastructure.Marshaling;

namespace DTasks.Extensions.DependencyInjection;

public interface IDTasksServiceConfiguration
{
    IDTasksServiceConfiguration UseTypeResolverBuilder(DAsyncTypeResolverBuilder typeResolverBuilder);

    IDTasksServiceConfiguration ConfigureTypeResolver(Action<DAsyncTypeResolverBuilder> configure);

    IDTasksServiceConfiguration RegisterDAsyncService(Type serviceType);

    IDTasksServiceConfiguration RegisterDAsyncService(Type serviceType, object? serviceKey);
}
