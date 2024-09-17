namespace DTasks.Extensions.Microsoft.DependencyInjection;

public interface IDTasksServiceConfiguration
{
    IDTasksServiceConfiguration RegisterDAsyncService(Type serviceType);
    
    IDTasksServiceConfiguration RegisterDAsyncService(Type serviceType, object? serviceKey);
}
