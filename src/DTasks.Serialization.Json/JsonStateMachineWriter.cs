using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Serialization.Json;

internal readonly ref struct JsonStateMachineWriter(IBufferWriter<byte> buffer, JsonSerializerOptions jsonOptions)
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

    public void SerializeStateMachine<TStateMachine>(ref TStateMachine stateMachine, TypeId typeId, ISuspensionContext context, IStateMachineSuspender<TStateMachine> suspender)
        where TStateMachine : notnull
    {
        _writer.WriteStartObject();
        _writer.WriteTypeIdProperty(typeId);
        _writer.WriteDAsyncId("@dtasks.pid", context.ParentId);
        suspender.Suspend(ref stateMachine, context, in this);
        _writer.WriteEndObject();
        _writer.Flush();
    }

    public void WriteField<TField>(string name, TField value)
    {
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
}
