namespace DTasks.Extensions.DependencyInjection.Configuration;

public interface IServiceConfigurationBuilder
{
    IServiceConfigurationBuilder RegisterDAsyncService<TService>();

    IServiceConfigurationBuilder RegisterDAsyncService<TService>(object? serviceKey);
}