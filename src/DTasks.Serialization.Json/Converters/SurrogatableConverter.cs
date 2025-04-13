using DTasks.Infrastructure.Marshaling;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
#if NET9_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace DTasks.Serialization.Json.Converters;

internal sealed class SurrogatableConverter<TSurrogatable>(JsonSerializerOptions globalOptions) : JsonConverter<TSurrogatable>
{
    public override TSurrogatable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (options.GetState().Surrogator is not IDAsyncSurrogator surrogator)
            return ReadNonSurrogated(ref reader, typeToConvert, options);

        if (reader.TokenType is not JsonTokenType.StartObject || !TryReadTypeId(ref reader, out TypeId typeId))
            return ReadNonSurrogated(ref reader, typeToConvert, options);

        reader.ExpectPropertyName("surrogate");
        reader.MoveNext();

#if NET9_0_OR_GREATER
        RestorationAction restorationAction = new(ref reader, options);
        if (!surrogator.TryRestore<TSurrogatable, RestorationAction>(typeId, ref restorationAction))
            throw new InvalidOperationException("Could not restore a surrogated value."); // TODO: Make consistent with similar errors in solution

        reader.ExpectToken(JsonTokenType.EndObject);

        return restorationAction.Value;
#else
        if (!surrogator.TryRestore<TSurrogatable>(typeId, out RestorationResult result))
            throw new InvalidOperationException("Could not restore a surrogated value."); // TODO: Make consistent with similar errors in solution

        object? surrogate = JsonSerializer.Deserialize(ref reader, result.SurrogateType, options);
        reader.MoveNext();

        reader.ExpectToken(JsonTokenType.EndObject);

        return result.Converter.Convert<object?, TSurrogatable>(surrogate);
#endif
    }

    public override void Write(Utf8JsonWriter writer, TSurrogatable value, JsonSerializerOptions options)
    {
        if (options.GetState().Surrogator is not IDAsyncSurrogator surrogator)
        {
            WriteNonSurrogated(writer, value, options);
            return;
        }

        ValueSurrogationAction surrogationAction = new(writer, options);
        if (!surrogator.TrySurrogate(in value, ref surrogationAction))
        {
            WriteNonSurrogated(writer, value, options);
        }
    }

    private TSurrogatable? ReadNonSurrogated(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var converter = GetDefaultConverter();
        return converter.Read(ref reader, typeToConvert, options);
    }

    private void WriteNonSurrogated(Utf8JsonWriter writer, TSurrogatable value, JsonSerializerOptions options)
    {
        var converter = GetDefaultConverter();
        converter.Write(writer, value, options);
    }

    private JsonConverter<TSurrogatable> GetDefaultConverter()
    {
        return (JsonConverter<TSurrogatable>)globalOptions.GetTypeInfo(typeof(TSurrogatable)).Converter;
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

#if NET9_0_OR_GREATER
    private ref struct RestorationAction(
        ref Utf8JsonReader reader,
        JsonSerializerOptions options) : IRestorationAction
    {
        private ref ReinterpretMeAsUtf8JsonReader _reader = ref Unsafe.As<Utf8JsonReader, ReinterpretMeAsUtf8JsonReader>(ref reader);

        public TSurrogatable? Value { get; private set; }

        public void RestoreAs<TConverter>(Type surrogateType, scoped ref TConverter converter)
            where TConverter : struct, ISurrogateConverter
        {
            object? surrogate = ReadSurrogate(surrogateType);
            Value = converter.Convert<object?, TSurrogatable>(surrogate);
        }

        public void RestoreAs(Type surrogateType, ISurrogateConverter converter)
        {
            object? surrogate = ReadSurrogate(surrogateType);
            Value = converter.Convert<object?, TSurrogatable>(surrogate);
        }

        private object? ReadSurrogate(Type surrogateType)
        {
            ref Utf8JsonReader reader = ref Unsafe.As<ReinterpretMeAsUtf8JsonReader, Utf8JsonReader>(ref _reader);
            object? surrogate = JsonSerializer.Deserialize(ref reader, surrogateType, options);
            reader.MoveNext();
            return surrogate;
        }

        // The compiler won't let us store a reference to the Utf8JsonReader, since it is a ref struct.
        // This allows us to bypass that check. At the same time, we must make sure that we don't misuse the reference.
        private struct ReinterpretMeAsUtf8JsonReader;
    }
#endif

    private readonly struct ValueSurrogationAction(Utf8JsonWriter writer, JsonSerializerOptions options) : ISurrogationAction
    {
        public void SurrogateAs<TSurrogate>(TypeId typeId, TSurrogate surrogate)
        {
            writer.WriteStartObject();

            writer.WriteTypeId("@dtasks.tid", typeId);

            writer.WritePropertyName("surrogate");
            JsonSerializer.Serialize(writer, surrogate, options);

            writer.WriteEndObject();
        }
    }
}
