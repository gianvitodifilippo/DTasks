using DTasks.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DTasks.Serialization.Json;

public sealed class FlowIdJsonConverter : JsonConverter<FlowId>
{
    public override FlowId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        byte[] bytes = reader.GetBytesFromBase64();
        if (!FlowId.TryReadBytes(bytes, out FlowId flowId))
            throw new JsonException("Invalid flow id value.");

        return flowId;
    }

    public override void Write(Utf8JsonWriter writer, FlowId value, JsonSerializerOptions options)
    {
        Span<byte> bytes = stackalloc byte[8];
        if (!value.TryWriteBytes(bytes))
            throw new InvalidOperationException("Could not serialize flow id.");

        writer.WriteBase64StringValue(bytes);
    }
}
