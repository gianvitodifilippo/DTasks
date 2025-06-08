using System.Buffers;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Serialization.Json.Converters;

internal sealed class SurrogatableConverter<TSurrogatable>(IDAsyncSurrogator surrogator, JsonSerializerOptions defaultOptions) : JsonConverter<TSurrogatable>
{
    public override TSurrogatable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.StartObject || !TryReadTypeId(ref reader, out TypeId typeId))
            return ReadNonSurrogated(ref reader, typeToConvert);

        reader.ExpectPropertyName("surrogate");
        reader.MoveNext();

#if NET9_0_OR_GREATER
        JsonUnmarshaller unmarshaller = new(ref reader, options);
        if (!surrogator.TryRestore(typeId, ref unmarshaller, out TSurrogatable? value))
            throw new InvalidOperationException("Could not restore a surrogated value."); // TODO: Make consistent with similar errors in solution

        reader.MoveNext();
        reader.ExpectToken(JsonTokenType.EndObject);

        return value;
#else
        Utf8JsonReader peekingReader = reader;
        int bufferSize = GetBufferSize(ref peekingReader);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            Span<byte> bufferSpan = buffer.AsSpan(0, bufferSize);
            FillBuffer(ref bufferSpan, ref reader);
            
            Debug.Assert(bufferSpan.Length == 0, "The buffer should have been filled.");
            Debug.Assert(reader.BytesConsumed == peekingReader.BytesConsumed, "Expected the reader to have consumed as many bytes as its copy.");
            JsonUnmarshaller unmarshaller = new(buffer, bufferSize, reader.CurrentState.Options, options);
            if (!surrogator.TryRestore(typeId, ref unmarshaller, out TSurrogatable? value))
                throw new InvalidOperationException("Could not restore a surrogated value."); // TODO: Make consistent with similar errors in solution
            
            reader.MoveNext();
            reader.ExpectToken(JsonTokenType.EndObject);
            return value;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
#endif
    }

    public override void Write(Utf8JsonWriter writer, TSurrogatable value, JsonSerializerOptions options)
    {
        JsonMarshaller marshaller = new(writer, options);
        if (surrogator.TrySurrogate(in value, ref marshaller))
            return;

        WriteNonSurrogated(writer, value);
    }

    private TSurrogatable? ReadNonSurrogated(ref Utf8JsonReader reader, Type typeToConvert)
    {
        return typeof(TSurrogatable).IsValueType
            ? JsonSerializer.Deserialize<TSurrogatable>(ref reader, defaultOptions)
            : (TSurrogatable?)JsonSerializer.Deserialize(ref reader, typeToConvert, defaultOptions);
    }

    private void WriteNonSurrogated(Utf8JsonWriter writer, TSurrogatable value)
    {
        JsonSerializer.Serialize(writer, value, defaultOptions);
    }

    private static bool TryReadTypeId(ref Utf8JsonReader reader, out TypeId typeId)
    {
        Utf8JsonReader peekingReader = reader;

        if (!peekingReader.Read())
            return False(out typeId);

        if (peekingReader.TokenType is not JsonTokenType.PropertyName)
            return False(out typeId);

        if (peekingReader.GetString() is not "@dtasks.tid")
            return False(out typeId);

        if (!peekingReader.Read())
            return False(out typeId);

        reader = peekingReader;

        typeId = reader.ReadTypeId();
        reader.MoveNext();

        return true;

        static bool False(out TypeId typeId)
        {
            typeId = default;
            return false;
        }
    }
    
#if !NET9_0_OR_GREATER
    private static int GetBufferSize(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.StartObject => GetObjectBufferSize(ref reader),
            JsonTokenType.StartArray => GetArrayBufferSize(ref reader),
            JsonTokenType.String => reader.ValueSpan.Length + 2 * "\""u8.Length,
            JsonTokenType.EndObject or JsonTokenType.EndArray => throw new JsonException($"Unexpected token type '{reader.TokenType}'."),
            _ => reader.ValueSpan.Length
        };

        static int GetObjectBufferSize(ref Utf8JsonReader reader)
        {
            int length = "{}"u8.Length;

            reader.MoveNext();
            while (reader.TokenType != JsonTokenType.EndObject)
            {
                reader.ExpectToken(JsonTokenType.PropertyName);
                length += reader.ValueSpan.Length + 2 * "\""u8.Length + ":"u8.Length;
                reader.MoveNext();

                length += GetBufferSize(ref reader);
                reader.MoveNext();
            }
            
            return length;
        }

        static int GetArrayBufferSize(ref Utf8JsonReader reader)
        {
            int length = "[]"u8.Length;

            reader.MoveNext();
            while (reader.TokenType != JsonTokenType.EndArray)
            {
                length += GetBufferSize(ref reader) + ","u8.Length;
                reader.MoveNext();
            }
            
            return length;
        }
    }

    private static void FillBuffer(ref Span<byte> buffer, ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.StartObject:
                FillBufferWithObject(ref buffer, ref reader);
                break;
            
            case JsonTokenType.StartArray:
                FillBufferWithArray(ref buffer, ref reader);
                break;
            
            case JsonTokenType.String:
                "\""u8.CopyTo(buffer);
                buffer = buffer["\""u8.Length..];
                reader.ValueSpan.CopyTo(buffer);
                buffer = buffer[reader.ValueSpan.Length..];
                "\""u8.CopyTo(buffer);
                buffer = buffer["\""u8.Length..];
                break;
            
            case JsonTokenType.EndObject:
            case JsonTokenType.EndArray:
                throw new JsonException($"Unexpected token type '{reader.TokenType}'.");
            
            default:
                reader.ValueSpan.CopyTo(buffer);
                break;
        }

        static void FillBufferWithObject(ref Span<byte> buffer, ref Utf8JsonReader reader)
        {
            "{"u8.CopyTo(buffer);
            buffer = buffer["{"u8.Length..];
            
            reader.MoveNext();
            while (reader.TokenType != JsonTokenType.EndObject)
            {
                reader.ExpectToken(JsonTokenType.PropertyName);
                "\""u8.CopyTo(buffer);
                buffer = buffer["\""u8.Length..];
                reader.ValueSpan.CopyTo(buffer);
                buffer = buffer[reader.ValueSpan.Length..];
                "\":"u8.CopyTo(buffer);
                buffer = buffer["\":"u8.Length..];
                
                FillBuffer(ref buffer, ref reader);
                reader.MoveNext();
                
                ","u8.CopyTo(buffer);
                buffer = buffer[","u8.Length..];
            }
            
            "}"u8.CopyTo(buffer);
            buffer = buffer["}"u8.Length..];
        }

        static void FillBufferWithArray(ref Span<byte> buffer, ref Utf8JsonReader reader)
        {
            "["u8.CopyTo(buffer);
            buffer = buffer["["u8.Length..];
            
            reader.MoveNext();
            while (reader.TokenType != JsonTokenType.EndArray)
            {
                FillBuffer(ref buffer, ref reader);
                reader.MoveNext();
                
                ","u8.CopyTo(buffer);
                buffer = buffer[","u8.Length..];
            }
            
            "]"u8.CopyTo(buffer);
            buffer = buffer["}"u8.Length..];
        }
    }
#endif
}
