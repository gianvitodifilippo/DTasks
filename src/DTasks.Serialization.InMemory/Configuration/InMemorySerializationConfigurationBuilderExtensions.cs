using DTasks.Configuration.DependencyInjection;
using DTasks.Serialization.InMemory;

namespace DTasks.Serialization.Configuration;

public static class InMemorySerializationConfigurationBuilderExtensions
{
    public static ISerializationConfigurationBuilder UseInMemoryStorage(this ISerializationConfigurationBuilder builder)
    {
        return builder.UseStorage(ComponentDescriptor.Singleton(new InMemoryDAsyncStorage()));
    }
}