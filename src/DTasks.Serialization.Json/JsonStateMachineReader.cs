using System.Text.Json;
using DTasks.Infrastructure;
using DTasks.Infrastructure.Generics;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using DTasks.Inspection;

namespace DTasks.Serialization.Json;

internal ref struct JsonStateMachineReader(ReadOnlySpan<byte> bytes, JsonSerializerOptions jsonOptions)
{
    private delegate TValue DeserializationAction<out TValue>(ref Utf8JsonReader reader, JsonSerializerOptions jsonOptions);
    public delegate IDAsyncRunnable ResumeAction<in T>(IStateMachineResumer resumer, T arg, ref JsonStateMachineReader reader);

    private Utf8JsonReader _reader = new(bytes);

    public DAsyncLink DeserializeStateMachine<T>(DAsyncId id, IStateMachineInspector inspector, IDAsyncTypeResolver typeResolver, T arg, ResumeAction<T> resume)
    {
        _reader.MoveNext();
        _reader.ExpectToken(JsonTokenType.StartObject);

        _reader.MoveNext();
        _reader.ExpectPropertyName("@dtasks.tid");

        _reader.MoveNext();
        TypeId typeId = _reader.ReadTypeId();
        
        ITypeContext typeContext = typeResolver.GetTypeContext(typeId);
        if (!typeContext.IsStateMachine)
            throw new InvalidOperationException($"Cannot resume '{id}' as it is in a terminal state.");

        _reader.MoveNext();
        _reader.ExpectPropertyName("@dtasks.pid");

        _reader.MoveNext();
        DAsyncId parentId = _reader.ReadDAsyncId();

        IStateMachineResumer resumer = (IStateMachineResumer)inspector.GetResumer(typeContext.Type);

        _reader.MoveNext();
        IDAsyncRunnable runnable = resume(resumer, arg, ref this);

        _reader.ExpectToken(JsonTokenType.EndObject);
        _reader.ExpectEnd();

        return new DAsyncLink(parentId, runnable);
    }

    public bool ReadField<TField>(string name, ref TField? value)
    {
        if (!CheckPropertyNameAndAdvance(name))
            return false;

        value = GetValueAndAdvance(static (ref Utf8JsonReader reader, JsonSerializerOptions jsonOptions) => JsonSerializer.Deserialize<TField>(ref reader, jsonOptions));
        return true;
    }

    public bool ReadField(string name, ref int value)
    {
        if (!CheckPropertyNameAndAdvance(name))
            return false;

        value = GetValueAndAdvance(static (ref Utf8JsonReader reader, JsonSerializerOptions _) => reader.GetInt32());
        return true;
    }

    public bool ReadField(string name, ref short value)
    {
        if (!CheckPropertyNameAndAdvance(name))
            return false;

        value = GetValueAndAdvance(static (ref Utf8JsonReader reader, JsonSerializerOptions _) => reader.GetInt16());
        return true;
    }

    public bool ReadField(string name, ref long value)
    {
        if (!CheckPropertyNameAndAdvance(name))
            return false;

        value = GetValueAndAdvance(static (ref Utf8JsonReader reader, JsonSerializerOptions _) => reader.GetInt64());
        return true;
    }

    public bool ReadField(string name, ref uint value)
    {
        if (!CheckPropertyNameAndAdvance(name))
            return false;

        value = GetValueAndAdvance(static (ref Utf8JsonReader reader, JsonSerializerOptions _) => reader.GetUInt32());
        return true;
    }

    public bool ReadField(string name, ref ushort value)
    {
        if (!CheckPropertyNameAndAdvance(name))
            return false;

        value = GetValueAndAdvance(static (ref Utf8JsonReader reader, JsonSerializerOptions _) => reader.GetUInt16());
        return true;
    }

    public bool ReadField(string name, ref ulong value)
    {
        if (!CheckPropertyNameAndAdvance(name))
            return false;

        value = GetValueAndAdvance(static (ref Utf8JsonReader reader, JsonSerializerOptions _) => reader.GetUInt64());
        return true;
    }

    public bool ReadField(string name, ref string? value)
    {
        if (!CheckPropertyNameAndAdvance(name))
            return false;

        value = GetValueAndAdvance(static (ref Utf8JsonReader reader, JsonSerializerOptions _) => reader.GetString());
        return true;
    }

    private bool CheckPropertyNameAndAdvance(string name)
    {
        if (_reader.TokenType is not JsonTokenType.PropertyName || !_reader.ValueTextEquals(name))
            return false;

        _reader.MoveNext();
        return true;
    }

    private TValue GetValueAndAdvance<TValue>(DeserializationAction<TValue> deserialize)
    {
        TValue value = deserialize(ref _reader, jsonOptions);
        _reader.MoveNext();

        return value;
    }
}
