using DTasks.Marshaling;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.Microsoft.Extensions.DependencyInjection;

public interface IDTasksServiceConfiguration
{
    IServiceCollection Services { get; }

    IDTasksServiceConfiguration UseTypeResolverBuilder(ITypeResolverBuilder typeResolverBuilder);

    IDTasksServiceConfiguration RegisterDAsyncService(Type serviceType);

    IDTasksServiceConfiguration RegisterDAsyncService(Type serviceType, object? serviceKey);
}
