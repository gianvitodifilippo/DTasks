using DTasks.Serialization.Json.Configuration;

namespace DTasks.Serialization.Configuration;

public static class JsonSerializationConfigurationBuilderExtensions
{
    public static TBuilder UseJsonFormat<TBuilder>(this TBuilder builder)
        where TBuilder : ISerializationConfigurationBuilder
    {
        JsonFormatConfigurationBuilder jsonFormatBuilder = new();

        return jsonFormatBuilder.Configure(builder);
    }

    public static TBuilder UseJsonFormat<TBuilder>(this TBuilder builder, Action<IJsonFormatConfigurationBuilder> configure)
        where TBuilder : ISerializationConfigurationBuilder
    {
        JsonFormatConfigurationBuilder jsonFormatBuilder = new();
        configure(jsonFormatBuilder);

        return jsonFormatBuilder.Configure(builder);
    }
}