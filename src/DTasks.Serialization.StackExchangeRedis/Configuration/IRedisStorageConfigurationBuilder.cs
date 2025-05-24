using DTasks.Configuration.DependencyInjection;
using StackExchange.Redis;

namespace DTasks.Configuration;

public interface IRedisStorageConfigurationBuilder
{
    IRedisStorageConfigurationBuilder UseDatabase(IComponentDescriptor<IDatabase> descriptor);
}
