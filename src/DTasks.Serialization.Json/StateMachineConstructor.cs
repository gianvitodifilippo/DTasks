using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace DTasks.Serialization.Json;

internal ref struct StateMachineConstructor(ReadOnlySpan<byte> bytes, ref readonly JsonFlowHeap heap)
{
    private Utf8JsonReader _reader = new Utf8JsonReader(bytes);

#if NET8_0_OR_GREATER

    private readonly ref readonly JsonFlowHeap _heap = ref heap;

    private readonly JsonSerializerOptions Options
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _heap.Options;
    }

    private readonly DTaskReferenceResolver ReferenceResolver
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _heap.ReferenceResolver;
    }

#else

    private readonly JsonSerializerOptions _options = heap.Options;
    private readonly DTaskReferenceResolver _referenceResolver = heap.ReferenceResolver;

    private readonly JsonSerializerOptions Options
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _options;
    }

    private readonly DTaskReferenceResolver ReferenceResolver
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _referenceResolver;
    }

#endif

    public void StartReading()
    {
        _reader.MoveNext();
        _reader.ExpectType(JsonTokenType.StartObject);
        _reader.MoveNext();
    }

    public void EndReading()
    {
        _reader.ExpectType(JsonTokenType.EndObject);
        _reader.ExpectEnd();
    }

    public object ReadTypeId()
    {
        if (!IsProperty(Constants.TypeMetadataKeyUtf8))
            throw new JsonException($"Expected property '{Constants.TypeMetadataKey}'.");

        _reader.MoveNext();
        object typeId = _reader.TokenType switch
        {
            JsonTokenType.String => _reader.GetString()!,
            JsonTokenType.Number => _reader.GetInt32(),
            _ => throw new JsonException($"Unsupported identifier type '{_reader.TokenType}'.")
        };

        _reader.MoveNext();
        return typeId;
    }

    public bool HandleField<TField>(string fieldName, ref TField? value)
    {
        if (!IsProperty(fieldName))
            return false;

        if (!typeof(TField).IsValueType)
            return HandleReference(ref value);

        _reader.MoveNext();
        value = JsonSerializer.Deserialize<TField>(ref _reader, Options);

        return Next();
    }

    public bool HandleAwaiter(string fieldName)
    {
        if (!IsProperty(fieldName))
            return false;

        _reader.MoveNext();
        _reader.ExpectType(JsonTokenType.String);
        if (!_reader.ValueTextEquals(Constants.AwaiterValueUtf8))
            throw new JsonException($"Expected awaiter value to be '{Constants.AwaiterValue}'.");

        return Next();
    }

    public bool HandleField(string fieldName, ref string? value)
    {
        if (!IsProperty(fieldName))
            return false;

        _reader.MoveNext();
        value = _reader.GetString();
        return Next();
    }

    public bool HandleField(string fieldName, ref byte value)
    {
        if (!IsProperty(fieldName))
            return false;

        _reader.MoveNext();
        value = _reader.GetByte();
        return Next();
    }

    public bool HandleField(string fieldName, ref sbyte value)
    {
        if (!IsProperty(fieldName))
            return false;

        _reader.MoveNext();
        value = _reader.GetSByte();
        return Next();
    }

    public bool HandleField(string fieldName, ref bool value)
    {
        if (!IsProperty(fieldName))
            return false;

        _reader.MoveNext();
        value = _reader.GetBoolean();
        return Next();
    }

    public bool HandleField(string fieldName, ref char value)
    {
        if (!IsProperty(fieldName))
            return false;

        _reader.MoveNext();
        string? stringValue = _reader.GetString();
        if (stringValue is null || stringValue.Length != 1)
            throw new JsonException("Expected a string of length 1.");

        value = stringValue[0];
        return Next();
    }

    public bool HandleField(string fieldName, ref short value)
    {
        if (!IsProperty(fieldName))
            return false;

        _reader.MoveNext();
        value = _reader.GetInt16();
        return Next();
    }

    public bool HandleField(string fieldName, ref int value)
    {
        if (!IsProperty(fieldName))
            return false;

        _reader.MoveNext();
        value = _reader.GetInt32();
        return Next();
    }

    public bool HandleField(string fieldName, ref long value)
    {
        if (!IsProperty(fieldName))
            return false;

        _reader.MoveNext();
        value = _reader.GetInt64();
        return Next();
    }

    public bool HandleField(string fieldName, ref ushort value)
    {
        if (!IsProperty(fieldName))
            return false;

        _reader.MoveNext();
        value = _reader.GetUInt16();
        return Next();
    }

    public bool HandleField(string fieldName, ref uint value)
    {
        if (!IsProperty(fieldName))
            return false;

        _reader.MoveNext();
        value = _reader.GetUInt32();
        return Next();
    }

    public bool HandleField(string fieldName, ref ulong value)
    {
        if (!IsProperty(fieldName))
            return false;

        _reader.MoveNext();
        value = _reader.GetUInt64();
        return Next();
    }

    public bool HandleField(string fieldName, ref float value)
    {
        if (!IsProperty(fieldName))
            return false;

        _reader.MoveNext();
        value = _reader.GetSingle();
        return Next();
    }

    public bool HandleField(string fieldName, ref double value)
    {
        if (!IsProperty(fieldName))
            return false;

        _reader.MoveNext();
        value = _reader.GetDouble();
        return Next();
    }

    public bool HandleField(string fieldName, ref decimal value)
    {
        if (!IsProperty(fieldName))
            return false;

        _reader.MoveNext();
        value = _reader.GetDecimal();
        return Next();
    }

    public bool HandleField(string fieldName, ref DateTime value)
    {
        if (!IsProperty(fieldName))
            return false;

        _reader.MoveNext();
        value = _reader.GetDateTime();
        return Next();
    }

    public bool HandleField(string fieldName, ref DateTimeOffset value)
    {
        if (!IsProperty(fieldName))
            return false;

        _reader.MoveNext();
        value = _reader.GetDateTimeOffset();
        return Next();
    }

    public bool HandleField(string fieldName, ref Guid value)
    {
        if (!IsProperty(fieldName))
            return false;

        _reader.MoveNext();
        value = _reader.GetGuid();
        return Next();
    }

    public bool HandleField(string fieldName, ref ReadOnlyMemory<byte> value)
    {
        throw new NotImplementedException();
    }

    private bool HandleReference<TField>(ref TField? value)
    {
        Debug.Assert(!typeof(TField).IsValueType, $"Expected {nameof(TField)} to be a reference type.");

        _reader.MoveNext();
        _reader.ExpectType(JsonTokenType.StartObject);

        _reader.MoveNext();
        if (!IsProperty(Constants.RefMetadataKeyUtf8))
            throw new JsonException($"Expected '{Constants.RefMetadataKey}' property.");

        _reader.MoveNext();
        string referenceId = _reader.GetString() ?? throw new JsonException($"Expected a reference id.");

        _reader.MoveNext();
        _reader.ExpectType(JsonTokenType.EndObject);

        object reference = ReferenceResolver.ResolveReference(referenceId);
        try
        {
            value = (TField?)reference;
        }
        catch (InvalidCastException)
        {
            throw new JsonException($"Reference of type '{reference.GetType()}' to '{typeof(TField)}'.");
        }

        return Next();
    }

    private bool Next()
    {
        _reader.MoveNext();
        return true;
    }

    private readonly bool IsProperty(string propertyName)
    {
        return _reader.TokenType is JsonTokenType.PropertyName && _reader.ValueTextEquals(propertyName);
    }

    private readonly bool IsProperty(ReadOnlySpan<byte> propertyName)
    {
        return _reader.TokenType is JsonTokenType.PropertyName && _reader.ValueTextEquals(propertyName);
    }
}