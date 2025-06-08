using System.Text.Json;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Serialization.Json;

internal struct JsonMarshaller(Utf8JsonWriter writer, JsonSerializerOptions options) : IMarshaller
{
    private int _writtenCount;
    
    public void WriteSurrogate<TSurrogate>(TypeId typeId, in TSurrogate value)
    {
        writer.WriteStartObject();
            
        writer.WriteTypeIdProperty(typeId);
            
        writer.WritePropertyName("surrogate");
        JsonSerializer.Serialize(writer, value, options);
            
        writer.WriteEndObject();
    }

    public void BeginArray(TypeId typeId, int memberCount)
    {
        writer.WriteStartObject();
            
        writer.WriteTypeIdProperty(typeId);
        
        writer.WritePropertyName("surrogate");
        writer.WriteStartArray();
    }

    public void EndArray()
    {
        writer.WriteEndArray();
    }

    public void WriteItem<T>(in T value)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
