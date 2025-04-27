using DTasks.Serialization.Configuration;

namespace DTasks.Configuration;

public static class SerializationDTasksConfigurationBuilderExtensions
{
    public static TBuilder UseSerialization<TBuilder>(this TBuilder builder, Action<ISerializationConfigurationBuilder> configure)
        where TBuilder : IDTasksConfigurationBuilder<TBuilder>
    {
        SerializationConfigurationBuilder serializationBuilder = new();
        configure(serializationBuilder);
        
        return serializationBuilder.Configure(builder);
    }
}