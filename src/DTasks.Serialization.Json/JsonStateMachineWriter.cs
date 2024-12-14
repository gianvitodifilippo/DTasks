using DTasks.Marshaling;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DTasks.Serialization.Json;

internal readonly ref struct JsonStateMachineWriter(
    Utf8JsonWriter writer,
    JsonSerializerOptions jsonOptions,
    ReferenceResolver referenceResolver,
    IDAsyncMarshaler marshaler)
{
    public void WriteField<TField>(string name, in TField value)
    {
        if (!typeof(TField).IsValueType)
        {
            if (value is not null)
            {
                ReferenceMarshalingAction marshalingAction = new(writer, jsonOptions, referenceResolver, value, name);
                if (marshaler.TryMarshal(in value, ref marshalingAction))
                    return;
            }
            else
            {
                if (jsonOptions.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull)
                    return;

                writer.WriteNull(name);
                return;
            }
        }
        else
        {
            ValueMarshalingAction marshalingAction = new(writer, jsonOptions, name);
            if (marshaler.TryMarshal(in value, ref marshalingAction))
                return;
        }

        writer.WritePropertyName(name);
        JsonSerializer.Serialize(writer, value, jsonOptions);
    }

    public void WriteField(string name, string value)
    {
        if (value is null && jsonOptions.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull)
            return;

        writer.WriteString(name, value);
    }

    public void WriteField(string name, bool value)
    {
        writer.WriteBoolean(name, value);
    }

    public void WriteField(string name, byte value)
    {
        writer.WriteNumber(name, value);
    }

    public void WriteField(string name, sbyte value)
    {
        writer.WriteNumber(name, value);
    }

    public void WriteField(string name, short value)
    {
        writer.WriteNumber(name, value);
    }

    public void WriteField(string name, ushort value)
    {
        writer.WriteNumber(name, value);
    }

    public void WriteField(string name, int value)
    {
        writer.WriteNumber(name, value);
    }

    public void WriteField(string name, uint value)
    {
        writer.WriteNumber(name, value);
    }

    public void WriteField(string name, long value)
    {
        writer.WriteNumber(name, value);
    }

    public void WriteField(string name, ulong value)
    {
        writer.WriteNumber(name, value);
    }

    public void HandleField(string name, float value)
    {
        writer.WriteNumber(name, value);
    }

    public void HandleField(string name, double value)
    {
        writer.WriteNumber(name, value);
    }

    public void HandleField(string name, decimal value)
    {
        writer.WriteNumber(name, value);
    }

    public void HandleField(string name, DateTime value)
    {
        writer.WriteString(name, value);
    }

    public void HandleField(string name, DateTimeOffset value)
    {
        writer.WriteString(name, value);
    }

    public void HandleField(string name, Guid value)
    {
        writer.WriteString(name, value);
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

        writer.WritePropertyName("typeId");
        JsonSerializer.Serialize(writer, typeId, jsonOptions);
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
