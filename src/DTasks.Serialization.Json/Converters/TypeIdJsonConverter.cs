using System.Text.Json;
using System.Text.Json.Serialization;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Serialization.Json.Converters;

public sealed class TypeIdJsonConverter : JsonConverter<TypeId>
{
    public override TypeId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.ReadTypeId();
    }

    public override void Write(Utf8JsonWriter writer, TypeId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
