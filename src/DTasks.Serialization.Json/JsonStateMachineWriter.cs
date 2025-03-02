using DTasks.Hosting;
using DTasks.Marshaling;
using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DTasks.Serialization.Json;

internal readonly ref struct JsonStateMachineWriter(
    IBufferWriter<byte> buffer,
    JsonSerializerOptions jsonOptions,
    ReferenceResolver referenceResolver,
    IDAsyncMarshaler marshaler)
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

    public void SerializeStateMachine<TStateMachine>(ref TStateMachine stateMachine, TypeId typeId, DAsyncId parentId, IStateMachineSuspender<TStateMachine> suspender, ISuspensionContext suspensionContext)
        where TStateMachine : notnull
    {
        _writer.WriteStartObject();
        _writer.WriteTypeId("$typeId", typeId);
        _writer.WriteDAsyncId("$parentId", parentId);
        suspender.Suspend(ref stateMachine, suspensionContext, in this);
        _writer.WriteEndObject();
        _writer.Flush();
    }

    public void WriteField<TField>(string name, TField value)
    {
        if (!typeof(TField).IsValueType)
        {
            if (value is not null)
            {
                ReferenceMarshalingAction marshalingAction = new(_writer, jsonOptions, referenceResolver, value, name);
                if (marshaler.TryMarshal(in value, ref marshalingAction))
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
            ValueMarshalingAction marshalingAction = new(_writer, jsonOptions, name);
            if (marshaler.TryMarshal(in value, ref marshalingAction))
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

    private static void WriteMarshaledPropertyName(Utf8JsonWriter writer, string name)
    {
        ReadOnlySpan<char> namePrefix = StateMachineJsonConstants.MarshaledValuePrefix;
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

    private static void WriteToken<TToken>(Utf8JsonWriter writer, TToken token, JsonSerializerOptions jsonOptions)
    {
        if (token is null)
            return;

        writer.WritePropertyName("token");
        JsonSerializer.Serialize(writer, token, jsonOptions);
    }

    private readonly struct ValueMarshalingAction(
        Utf8JsonWriter writer,
        JsonSerializerOptions jsonOptions,
        string name) : IMarshalingAction
    {
        public void MarshalAs<TToken>(TypeId typeId, TToken token)
        {
            WriteMarshaledPropertyName(writer, name);
            
            writer.WriteStartObject();
            WriteTypeId(writer, typeId, jsonOptions);
            WriteToken(writer, token, jsonOptions);
            writer.WriteEndObject();
        }
    }

    private readonly struct ReferenceMarshalingAction(
        Utf8JsonWriter writer,
        JsonSerializerOptions jsonOptions,
        ReferenceResolver referenceResolver,
        object reference,
        string name) : IMarshalingAction
    {
        public void MarshalAs<TToken>(TypeId typeId, TToken token)
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

            WriteMarshaledPropertyName(writer, name);

            writer.WriteStartObject();
            writer.WriteString("$id", referenceId);
            WriteTypeId(writer, typeId, jsonOptions);
            WriteToken(writer, token, jsonOptions);
            writer.WriteEndObject();
        }
    }
}
