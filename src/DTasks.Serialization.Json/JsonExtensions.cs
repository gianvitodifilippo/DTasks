using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Text.Json;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Serialization.Json;

internal static class JsonExtensions
{
    private static ReadOnlySpan<byte> TypeIdPropertyName => "@dtasks.tid"u8;
    
    public static void WriteTypeIdProperty(this Utf8JsonWriter writer, TypeId typeId)
    {
        writer.WriteString(TypeIdPropertyName, typeId.ToString());
    }

    public static void WriteDAsyncId(this Utf8JsonWriter writer, string propertyName, DAsyncId id)
    {
        Span<byte> bytes = stackalloc byte[3 * sizeof(uint)];
        bool success = id.TryWriteBytes(bytes);
        Debug.Assert(success);

        writer.WriteBase64String(propertyName, bytes);
    }

    public static TypeId ReadTypeId(this ref Utf8JsonReader reader)
    {
        reader.ExpectToken(JsonTokenType.String, JsonTokenType.Null);
        string? typeIdValue = reader.GetString();

        if (typeIdValue is null)
            return default;

        return TypeId.Parse(typeIdValue);
    }

    public static DAsyncId ReadDAsyncId(this ref Utf8JsonReader reader)
    {
        reader.ExpectToken(JsonTokenType.String);

        Span<byte> bytes = stackalloc byte[3 * sizeof(uint)];
        OperationStatus status = Base64.DecodeFromUtf8(reader.ValueSpan, bytes, out int bytesConsumed, out int bytesWritten);

        if (status != OperationStatus.Done)
            throw new JsonException("Expected a base-64 value.");

        Debug.Assert(bytesConsumed == reader.ValueSpan.Length);

        if (!DAsyncId.TryReadBytes(bytes, out DAsyncId id))
            throw new JsonException("Invalid d-async id detected.");

        return id;
    }
}
