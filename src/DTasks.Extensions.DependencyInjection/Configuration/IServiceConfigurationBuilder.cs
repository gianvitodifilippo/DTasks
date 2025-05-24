namespace DTasks.Configuration;

public interface IServiceConfigurationBuilder
{
    IServiceConfigurationBuilder RegisterDAsyncService<TService>();

    IServiceConfigurationBuilder RegisterDAsyncService<TService>(object? serviceKey);
}