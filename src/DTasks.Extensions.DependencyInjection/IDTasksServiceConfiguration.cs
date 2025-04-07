namespace DTasks.Extensions.DependencyInjection;

public interface IDTasksServiceConfiguration
{
    IDTasksServiceConfiguration UseTypeResolverBuilder(ITypeResolverBuilder typeResolverBuilder);

    IDTasksServiceConfiguration ConfigureTypeResolver(Action<ITypeResolverBuilder> configure);

    IDTasksServiceConfiguration RegisterDAsyncService(Type serviceType);

    IDTasksServiceConfiguration RegisterDAsyncService(Type serviceType, object? serviceKey);
}
