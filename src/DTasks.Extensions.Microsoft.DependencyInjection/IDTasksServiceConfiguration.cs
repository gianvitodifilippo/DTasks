using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

public interface IDTasksServiceConfiguration
{
    IServiceCollection Services { get; }

    IDTasksServiceConfiguration RegisterDAsyncService(Type serviceType);
    
    IDTasksServiceConfiguration RegisterDAsyncService(Type serviceType, object? serviceKey);
}
