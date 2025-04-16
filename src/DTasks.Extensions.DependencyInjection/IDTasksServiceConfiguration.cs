using DTasks.Infrastructure.Marshaling;

namespace DTasks.Extensions.DependencyInjection;

public interface IDTasksServiceConfiguration
{
    IDTasksServiceConfiguration UseTypeResolverBuilder(IDAsyncTypeResolverBuilder typeResolverBuilder);

    IDTasksServiceConfiguration ConfigureTypeResolver(Action<IDAsyncTypeResolverBuilder> configure);

    IDTasksServiceConfiguration RegisterDAsyncService(Type serviceType);

    IDTasksServiceConfiguration RegisterDAsyncService(Type serviceType, object? serviceKey);
}
