using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Serialization.Json;

internal sealed class JsonDAsyncSerializer(JsonSerializerOptions serializerOptions) : IDAsyncSerializer
{
    public void Serialize<TValue>(IBufferWriter<byte> buffer, TValue value)
    {
        using Utf8JsonWriter writer = new(buffer, new JsonWriterOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            IndentCharacter = serializerOptions.IndentCharacter,
            Indented = serializerOptions.WriteIndented,
            IndentSize = serializerOptions.IndentSize,
            MaxDepth = serializerOptions.MaxDepth,
            NewLine = serializerOptions.NewLine,
            SkipValidation =
#if DEBUG
                true
#else
                false
#endif
        });
        writer.Dispose();

        JsonSerializer.Serialize(writer, value, serializerOptions);
    }

    public TValue Deserialize<TValue>(ReadOnlySpan<byte> bytes)
    {
        Utf8JsonReader reader = new(bytes);
        return JsonSerializer.Deserialize<TValue>(ref reader, serializerOptions)!;
    }
}