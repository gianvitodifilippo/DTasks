using System.Text;
using System.Text.Json;

namespace DTasks.Serialization.Json;

internal static class Utf8JsonReaderExtensions
{
    public static void MoveNext(this ref Utf8JsonReader reader)
    {
        if (!reader.Read())
            throw new JsonException("Unexpected end of json.");
    }

    public static void ExpectEnd(this ref Utf8JsonReader reader)
    {
        if (reader.Read())
            throw new JsonException("Expected end of json.");
    }

    public static void ExpectToken(this ref readonly Utf8JsonReader reader, JsonTokenType expectedType)
    {
        JsonTokenType actualType = reader.TokenType;

        if (actualType != expectedType)
            throw new JsonException($"Expected token type '{expectedType}', got '{actualType}' instead.");
    }

    public static void ExpectPropertyName(this ref readonly Utf8JsonReader reader, string expectedName)
    {
        reader.ExpectToken(JsonTokenType.PropertyName);

        if (!reader.ValueTextEquals(expectedName))
            throw new JsonException($"Expected property '{expectedName}'.");
    }

    public static void ExpectPropertyName(this ref readonly Utf8JsonReader reader, ReadOnlySpan<byte> expectedName)
    {
        reader.ExpectToken(JsonTokenType.PropertyName);

        if (!reader.ValueTextEquals(expectedName))
            throw new JsonException($"Expected property '{Encoding.UTF8.GetString(expectedName)}'.");
    }
}