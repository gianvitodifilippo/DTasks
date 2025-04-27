using DTasks.Serialization.Json.Configuration;

namespace DTasks.Serialization.Configuration;

public static class JsonSerializationConfigurationBuilderExtensions
{
    public static ISerializationConfigurationBuilder UseJsonFormat(this ISerializationConfigurationBuilder builder)
    {
        JsonFormatConfigurationBuilder jsonFormatBuilder = new();
        
        return jsonFormatBuilder.Configure(builder);
    }
    
    public static ISerializationConfigurationBuilder UseJsonFormat(this ISerializationConfigurationBuilder builder, Action<IJsonFormatConfigurationBuilder> configure)
    {
        JsonFormatConfigurationBuilder jsonFormatBuilder = new();
        configure(jsonFormatBuilder);
        
        return jsonFormatBuilder.Configure(builder);
    }
}