using DTasks.Infrastructure;
using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Serialization.Json;

internal readonly ref struct JsonStateMachineWriter(
    IBufferWriter<byte> buffer,
    JsonSerializerOptions jsonOptions,
    ReferenceResolver referenceResolver,
    IDAsyncSurrogator surrogator)
{
    private readonly Utf8JsonWriter _writer = new(buffer, new JsonWriterOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        IndentCharacter = jsonOptions.IndentCharacter,
        Indented = jsonOptions.WriteIndented,
        IndentSize = jsonOptions.IndentSize,
        MaxDepth = jsonOptions.MaxDepth,
        NewLine = jsonOptions.NewLine,
        SkipValidation =
#if DEBUG
            true
#else
            false
#endif
    });

    public void SerializeStateMachine<TStateMachine>(ref TStateMachine stateMachine, TypeId typeId, ISuspensionContext context, DAsyncId parentId, IStateMachineSuspender<TStateMachine> suspender)
        where TStateMachine : notnull
    {
        _writer.WriteStartObject();
        _writer.WriteTypeId("$typeId", typeId);
        _writer.WriteDAsyncId("$parentId", parentId);
        suspender.Suspend(ref stateMachine, context, in this);
        _writer.WriteEndObject();
        _writer.Flush();
    }

    public void WriteField<TField>(string name, TField value)
    {
        if (!typeof(TField).IsValueType)
        {
            if (value is not null)
            {
                ReferenceSurrogationAction surrogationAction = new(_writer, jsonOptions, referenceResolver, value, name);
                if (surrogator.TrySurrogate(in value, ref surrogationAction))
                    return;
            }
            else
            {
                if (jsonOptions.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull)
                    return;

                _writer.WriteNull(name);
                return;
            }
        }
        else
        {
            ValueSurrogationAction surrogationAction = new(_writer, jsonOptions, name);
            if (surrogator.TrySurrogate(in value, ref surrogationAction))
                return;
        }

        _writer.WritePropertyName(name);
        JsonSerializer.Serialize(_writer, value, jsonOptions);
    }

    public void WriteField(string name, string? value)
    {
        if (value is null && jsonOptions.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull)
            return;

        _writer.WriteString(name, value);
    }

    public void WriteField(string name, bool value)
    {
        _writer.WriteBoolean(name, value);
    }

    public void WriteField(string name, byte value)
    {
        _writer.WriteNumber(name, value);
    }

    public void WriteField(string name, sbyte value)
    {
        _writer.WriteNumber(name, value);
    }

    public void WriteField(string name, short value)
    {
        _writer.WriteNumber(name, value);
    }

    public void WriteField(string name, ushort value)
    {
        _writer.WriteNumber(name, value);
    }

    public void WriteField(string name, int value)
    {
        _writer.WriteNumber(name, value);
    }

    public void WriteField(string name, uint value)
    {
        _writer.WriteNumber(name, value);
    }

    public void WriteField(string name, long value)
    {
        _writer.WriteNumber(name, value);
    }

    public void WriteField(string name, ulong value)
    {
        _writer.WriteNumber(name, value);
    }

    public void HandleField(string name, float value)
    {
        _writer.WriteNumber(name, value);
    }

    public void HandleField(string name, double value)
    {
        _writer.WriteNumber(name, value);
    }

    public void HandleField(string name, decimal value)
    {
        _writer.WriteNumber(name, value);
    }

    public void HandleField(string name, DateTime value)
    {
        _writer.WriteString(name, value);
    }

    public void HandleField(string name, DateTimeOffset value)
    {
        _writer.WriteString(name, value);
    }

    public void HandleField(string name, Guid value)
    {
        _writer.WriteString(name, value);
    }

    private static void WriteSurrogatedPropertyName(Utf8JsonWriter writer, string name)
    {
        ReadOnlySpan<char> namePrefix = StateMachineJsonConstants.SurrogatedValuePrefix;
        if (name.AsSpan().StartsWith(namePrefix))
            throw new NotSupportedException($"Prefix '{namePrefix.ToString()}' is reserved."); // TODO: Instead of throwing, escape the name

        int prefixLength = namePrefix.Length;
        Span<char> finalName = stackalloc char[prefixLength + name.Length];
        namePrefix.CopyTo(finalName);
        name.AsSpan().CopyTo(finalName[prefixLength..]);

        writer.WritePropertyName(finalName);
    }

    private static void WriteTypeId(Utf8JsonWriter writer, TypeId typeId, JsonSerializerOptions jsonOptions)
    {
        if (typeId == default)
            return;

        writer.WriteTypeId("typeId", typeId);
    }

    private static void WriteSurrogate<TSurrogate>(Utf8JsonWriter writer, TSurrogate surrogate, JsonSerializerOptions jsonOptions)
    {
        if (surrogate is null)
            return;

        writer.WritePropertyName("surrogate");
        JsonSerializer.Serialize(writer, surrogate, jsonOptions);
    }

    private readonly struct ValueSurrogationAction(
        Utf8JsonWriter writer,
        JsonSerializerOptions jsonOptions,
        string name) : ISurrogationAction
    {
        public void SurrogateAs<TSurrogate>(TypeId typeId, TSurrogate surrogate)
        {
            WriteSurrogatedPropertyName(writer, name);

            writer.WriteStartObject();
            WriteTypeId(writer, typeId, jsonOptions);
            WriteSurrogate(writer, surrogate, jsonOptions);
            writer.WriteEndObject();
        }
    }

    private readonly struct ReferenceSurrogationAction(
        Utf8JsonWriter writer,
        JsonSerializerOptions jsonOptions,
        ReferenceResolver referenceResolver,
        object reference,
        string name) : ISurrogationAction
    {
        public void SurrogateAs<TSurrogate>(TypeId typeId, TSurrogate surrogate)
        {
            string referenceId = referenceResolver.GetReference(reference, out bool alreadyExists);
            if (alreadyExists)
            {
                writer.WritePropertyName(name);
                writer.WriteStartObject();
                writer.WriteString("$ref", referenceId);
                writer.WriteEndObject();
                return;
            }

            WriteSurrogatedPropertyName(writer, name);

            writer.WriteStartObject();
            writer.WriteString("$id", referenceId);
            WriteTypeId(writer, typeId, jsonOptions);
            WriteSurrogate(writer, surrogate, jsonOptions);
            writer.WriteEndObject();
        }
    }
}
