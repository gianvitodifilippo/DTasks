namespace DTasks.Extensions.DependencyInjection.Configuration;

public interface IServiceConfigurationBuilder
{
    IServiceConfigurationBuilder RegisterAllServices(bool registerAll = true);

    IServiceConfigurationBuilder RegisterDAsyncService(Type serviceType);

    IServiceConfigurationBuilder RegisterDAsyncService(Type serviceType, object? serviceKey);
}