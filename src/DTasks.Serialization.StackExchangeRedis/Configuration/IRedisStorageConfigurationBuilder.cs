using DTasks.Configuration.DependencyInjection;
using StackExchange.Redis;

namespace DTasks.Serialization.StackExchangeRedis.Configuration;

public interface IRedisStorageConfigurationBuilder
{
    IRedisStorageConfigurationBuilder UseDatabase(IComponentDescriptor<IDatabase> descriptor);
}
