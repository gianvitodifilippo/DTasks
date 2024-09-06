using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DTasks.Serialization.Json;

internal readonly ref struct StateMachineDeconstructor(ref readonly JsonFlowHeap heap)
{
#if NET8_0_OR_GREATER
    private readonly ref readonly JsonFlowHeap _heap = ref heap;

    private Utf8JsonWriter Writer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _heap.Writer;
    }

    private readonly JsonSerializerOptions Options
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _heap.Options;
    }

    private readonly ReferenceResolver ReferenceResolver
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _heap.ReferenceResolver;
    }
#else
    private readonly Utf8JsonWriter _writer = heap.Writer;
    private readonly JsonSerializerOptions _options = heap.Options;
    private readonly ReferenceResolver _referenceResolver = heap.ReferenceResolver;

    private Utf8JsonWriter Writer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _writer;
    }

    private readonly JsonSerializerOptions Options
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _options;
    }

    private readonly ReferenceResolver ReferenceResolver
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _referenceResolver;
    }
#endif

    public void StartWriting()
    {
        Writer.WriteStartObject();
    }

    public void EndWriting()
    {
        Writer.WriteEndObject();
    }

    public void WriteTypeId(object typeId)
    {
        switch (typeId)
        {
            case string stringId:
                Writer.WriteString(Constants.TypeMetadataKey, stringId);
                break;

            case int intId:
                Writer.WriteNumber(Constants.TypeMetadataKey, intId);
                break;

            default: // TODO: Support more identifier types
                throw new InvalidOperationException("Unsupported identifier type.");
        }
    }

    public void OnField<TField>(string fieldName, TField? value)
    {
        if (!typeof(TField).IsValueType)
        {
            WriteReference(fieldName, value);
            return;
        }

        Writer.WritePropertyName(fieldName);
        JsonSerializer.Serialize(Writer, value, Options);
    }

    public void OnAwaiter(string fieldName)
    {
        Writer.WriteString(fieldName, Constants.AwaiterValueUtf8);
    }

    public void OnField(string fieldName, byte value)
    {
        Writer.WriteNumber(fieldName, value);
    }

    public void OnField(string fieldName, sbyte value)
    {
        Writer.WriteNumber(fieldName, value);
    }

    public void OnField(string fieldName, bool value)
    {
        Writer.WriteBoolean(fieldName, value);
    }

    public void OnField(string fieldName, char value)
    {
        Writer.WriteString(fieldName, value.ToString());
    }

    public void OnField(string fieldName, short value)
    {
        Writer.WriteNumber(fieldName, value);
    }

    public void OnField(string fieldName, int value)
    {
        Writer.WriteNumber(fieldName, value);
    }

    public void OnField(string fieldName, long value)
    {
        Writer.WriteNumber(fieldName, value);
    }

    public void OnField(string fieldName, ushort value)
    {
        Writer.WriteNumber(fieldName, value);
    }

    public void OnField(string fieldName, uint value)
    {
        Writer.WriteNumber(fieldName, value);
    }

    public void OnField(string fieldName, ulong value)
    {
        Writer.WriteNumber(fieldName, value);
    }

    public void OnField(string fieldName, float value)
    {
        Writer.WriteNumber(fieldName, value);
    }

    public void OnField(string fieldName, double value)
    {
        Writer.WriteNumber(fieldName, value);
    }

    public void OnField(string fieldName, decimal value)
    {
        Writer.WriteNumber(fieldName, value);
    }

    public void OnField(string fieldName, DateTime value)
    {
        Writer.WriteString(fieldName, value);
    }

    public void OnField(string fieldName, DateTimeOffset value)
    {
        Writer.WriteString(fieldName, value);
    }

    public void OnField(string fieldName, Guid value)
    {
        Writer.WriteString(fieldName, value);
    }

    public void OnField(string fieldName, ReadOnlyMemory<byte> value)
    {
        throw new NotImplementedException();
    }

    private void WriteReference(string fieldName, object? value)
    {
        if (value is null) // This can be made configurable
            return;

        string referenceId = ReferenceResolver.GetReference(value, out _);

        Writer.WritePropertyName(fieldName);
        Writer.WriteStartObject();
        Writer.WriteString(Constants.RefMetadataKeyUtf8, referenceId);
        Writer.WriteEndObject();
    }
}
