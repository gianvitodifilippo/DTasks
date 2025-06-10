using System.Text.Json;
using System.Text.Json.Serialization;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Serialization.Json;

internal readonly ref struct JsonStateMachineWriter(Utf8JsonWriter writer, JsonSerializerOptions jsonOptions)
{
    public void SerializeStateMachine<TStateMachine>(ref TStateMachine stateMachine, TypeId typeId, IDehydrationContext context, IStateMachineSuspender<TStateMachine> suspender)
        where TStateMachine : notnull
    {
        writer.WriteStartObject();
        writer.WriteTypeIdProperty(typeId);
        writer.WriteDAsyncId("@dtasks.pid", context.ParentId);
        suspender.Suspend(ref stateMachine, context, in this);
        writer.WriteEndObject();
        writer.Flush();
    }

    public void WriteField<TField>(string name, TField value)
    {
        writer.WritePropertyName(name);
        JsonSerializer.Serialize(writer, value, jsonOptions);
    }

    public void WriteField(string name, string? value)
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
}
