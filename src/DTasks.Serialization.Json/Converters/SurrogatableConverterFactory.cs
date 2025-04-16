using DTasks.Infrastructure.Marshaling;
using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DTasks.Serialization.Json.Converters;

internal sealed class SurrogatableConverterFactory(FrozenSet<Type> surrogatableGenericTypes, object[] converterConstructorArguments) : JsonConverterFactory
{
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type converterType = typeof(SurrogatableConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType, converterConstructorArguments)!;
    }

    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType && surrogatableGenericTypes.Contains(typeToConvert.GetGenericTypeDefinition());
    }
}
