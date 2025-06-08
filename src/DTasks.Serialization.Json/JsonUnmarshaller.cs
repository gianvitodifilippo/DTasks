using System.Runtime.CompilerServices;
using System.Text.Json;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Serialization.Json;

#if NET9_0_OR_GREATER

internal ref struct JsonUnmarshaller(ref Utf8JsonReader reader, JsonSerializerOptions options) : IUnmarshaller
{
    private readonly ref ReinterpretMeAsUtf8JsonReader _reader = ref Unsafe.As<Utf8JsonReader, ReinterpretMeAsUtf8JsonReader>(ref reader);
    
    private ref Utf8JsonReader Reader => ref Unsafe.As<ReinterpretMeAsUtf8JsonReader, Utf8JsonReader>(ref _reader);
    
    public TSurrogate ReadSurrogate<TSurrogate>(Type surrogateType)
    {
        return (TSurrogate)JsonSerializer.Deserialize(ref Reader, surrogateType, options)!;
    }

    public void BeginArray()
    {
        ref Utf8JsonReader reader = ref Reader;
        reader.ExpectToken(JsonTokenType.StartArray);
        reader.MoveNext();
    }

    public void EndArray()
    {
        ref Utf8JsonReader reader = ref Reader;
        reader.ExpectToken(JsonTokenType.EndArray);
        reader.MoveNext();
    }

    public T ReadItem<T>()
    {
        return JsonSerializer.Deserialize<T>(ref Reader, options)!;
    }

    // The compiler won't let us store a reference to the Utf8JsonReader, since it is a ref struct.
    // This allows us to bypass that check. At the same time, we must make sure that we don't misuse the reference.
    private struct ReinterpretMeAsUtf8JsonReader;
}

#else

internal struct JsonUnmarshaller(byte[] buffer, int bufferSize, JsonReaderOptions readerOptions, JsonSerializerOptions options) : IUnmarshaller
{
    private JsonReaderState _readerState = new(readerOptions);
    private Utf8JsonReader Reader => new(buffer.AsSpan(0, bufferSize), isFinalBlock: true, _readerState);
    
    public TSurrogate ReadSurrogate<TSurrogate>(Type surrogateType)
    {
        Utf8JsonReader reader = Reader;
        return (TSurrogate)JsonSerializer.Deserialize(ref reader, surrogateType, options)!;
    }

    public void BeginArray()
    {
        Utf8JsonReader reader = Reader;
        reader.ExpectToken(JsonTokenType.StartArray);
        reader.MoveNext();
        _readerState = reader.CurrentState;
    }

    public void EndArray()
    {
        Utf8JsonReader reader = Reader;
        reader.ExpectToken(JsonTokenType.EndArray);
        reader.MoveNext();
        _readerState = reader.CurrentState;
    }

    public T ReadItem<T>()
    {
        Utf8JsonReader reader = Reader;
        T value = JsonSerializer.Deserialize<T>(ref reader, options)!;
        _readerState = reader.CurrentState;
        return value;
    }
}

#endif