using System.Diagnostics;
using System.Text;
using System.Text.Json;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Serialization.Json;

internal static class JsonExtensions // TODO: Standardize
{
    private static ReadOnlySpan<byte> TypeIdPropertyName => "@dtasks.tid"u8;
    
    public static void WriteTypeIdProperty(this Utf8JsonWriter writer, TypeId typeId)
    {
        writer.WriteString(TypeIdPropertyName, typeId.ToString());
    }

    public static void WriteDAsyncId(this Utf8JsonWriter writer, string propertyName, DAsyncId id)
    {
        Span<char> chars = stackalloc char[DAsyncId.CharCount];
        bool success = id.TryWriteChars(chars);
        Debug.Assert(success);
        
        writer.WriteString(propertyName, chars);
    }

    public static TypeId ReadTypeId(this ref Utf8JsonReader reader)
    {
        // TODO: Throw JsonException if value is malformed
        reader.ExpectToken(JsonTokenType.String, JsonTokenType.Null);
        string? typeIdValue = reader.GetString();

        if (typeIdValue is null)
            return default;

        return TypeId.Parse(typeIdValue);
    }

    public static DAsyncId ReadDAsyncId(this ref Utf8JsonReader reader)
    {
        reader.ExpectToken(JsonTokenType.String);

        Span<char> chars = stackalloc char[DAsyncId.CharCount];
        int charsWritten = Encoding.UTF8.GetChars(reader.ValueSpan, chars); // TODO: TryGetChars for .NET8 and .NET9
        if (charsWritten != DAsyncId.CharCount || !DAsyncId.TryParse(chars, out DAsyncId id))
            throw new JsonException("Invalid DAsyncId format.");
        
        return id;
    }
}
