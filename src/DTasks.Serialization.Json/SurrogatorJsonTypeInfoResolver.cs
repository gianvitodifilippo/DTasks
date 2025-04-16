using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace DTasks.Serialization.Json;

internal sealed class SurrogatorJsonTypeInfoResolver(JsonSerializerOptions surrogatorOptions) : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        JsonTypeInfo typeInfo = base.GetTypeInfo(type, options);

        foreach (JsonPropertyInfo propertyInfo in typeInfo.Properties)
        {
            if (propertyInfo.CustomConverter is not null)
                continue;

            propertyInfo.CustomConverter = surrogatorOptions.GetConverter(propertyInfo.PropertyType);
        }

        return typeInfo;
    }
}
