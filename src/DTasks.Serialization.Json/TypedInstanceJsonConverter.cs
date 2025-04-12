using System.Text.Json;
using System.Text.Json.Serialization;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Serialization.Json;

public sealed class TypedInstanceJsonConverter<TValue>(IDAsyncTypeResolver typeResolver) : JsonConverter<TypedInstance<TValue>>
    where TValue : class
{
    private static ReadOnlySpan<byte> TypeIdPropertyName => "t"u8;
    private static ReadOnlySpan<byte> ValuePropertyName => "v"u8;
    
    public override TypedInstance<TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Type? type = null;
        
        reader.ExpectToken(JsonTokenType.StartObject);
        reader.MoveNext();
        
        reader.ExpectToken(JsonTokenType.PropertyName);
        if (reader.ValueTextEquals(TypeIdPropertyName))
        {
            reader.MoveNext();
            TypeId typeId = JsonSerializer.Deserialize<TypeId>(ref reader, options);
            reader.MoveNext();
            
            type = typeResolver.GetType(typeId);
        }
        
        reader.ExpectPropertyName(ValuePropertyName);
        reader.MoveNext();
        
        TValue? value = type is null
            ? JsonSerializer.Deserialize<TValue>(ref reader, options)
            : (TValue?)JsonSerializer.Deserialize(ref reader, type, options);
        
        reader.ExpectToken(JsonTokenType.EndObject);
        reader.MoveNext();
        
        return value is null
            ? default
            : new TypedInstance<TValue>(type, value);
    }

    public override void Write(Utf8JsonWriter writer, TypedInstance<TValue> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        
        if (value.Type is not null)
        {
            TypeId typeId = typeResolver.GetTypeId(value.Type);
            
            writer.WritePropertyName(TypeIdPropertyName);
            JsonSerializer.Serialize(writer, typeId, options);
        }
        
        writer.WritePropertyName(ValuePropertyName);
        JsonSerializer.Serialize(writer, value.Value, options);
        
        writer.WriteEndObject();
    }
}