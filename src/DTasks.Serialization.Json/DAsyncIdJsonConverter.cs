using DTasks.Infrastructure;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DTasks.Serialization.Json;

public sealed class DAsyncIdJsonConverter : JsonConverter<DAsyncId>
{
    public override DAsyncId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.ReadDAsyncId();
    }

    public override void Write(Utf8JsonWriter writer, DAsyncId value, JsonSerializerOptions options)
    {
        Span<byte> bytes = stackalloc byte[3 * sizeof(uint)];
        bool success = value.TryWriteBytes(bytes);
        Debug.Assert(success);

        writer.WriteBase64StringValue(bytes);
    }
}
