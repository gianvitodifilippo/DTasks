namespace DTasks.Extensions.DependencyInjection.Configuration;

public interface IServiceConfigurationBuilder
{
    IServiceConfigurationBuilder RegisterDAsyncService(Type serviceType);

    IServiceConfigurationBuilder RegisterDAsyncService(Type serviceType, object? serviceKey);
}