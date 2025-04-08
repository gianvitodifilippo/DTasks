using System.Text.Json;
using System.Text.Json.Serialization;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Serialization.Json;

public class TypedInstanceJsonConverter<T>(IDAsyncTypeResolver typeResolver) : JsonConverter<TypedInstance<T>>
{
    public override TypedInstance<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, TypedInstance<T> value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}