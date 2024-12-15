using DTasks.Marshaling;
using System.Text.Json;
using System.Text.Json.Serialization;
using DTasks.Hosting;
using DTasks.Inspection;

#if NET9_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace DTasks.Serialization.Json;

internal ref struct JsonStateMachineReader(
    ReadOnlySpan<byte> bytes,
    JsonSerializerOptions jsonOptions,
    ReferenceResolver referenceResolver,
    IDAsyncMarshaler marshaler)
{
    public delegate IDAsyncRunnable ResumeAction<T>(IStateMachineResumer resumer, T arg, ref JsonStateMachineReader reader);

    private Utf8JsonReader _reader = new(bytes);

    public DAsyncLink DeserializeStateMachine<T>(IStateMachineInspector inspector, ITypeResolver typeResolver, T arg, ResumeAction<T> resume)
    {
        _reader.MoveNext();
        _reader.ExpectToken(JsonTokenType.StartObject);

        _reader.MoveNext();
        _reader.ExpectPropertyName("$typeId");

        _reader.MoveNext();
        TypeId typeId = _reader.ReadTypeId();

        _reader.MoveNext();
        _reader.ExpectPropertyName("$parentId");

        _reader.MoveNext();
        DAsyncId parentId = _reader.ReadDAsyncId();

        Type stateMachineType = typeResolver.GetType(typeId);

        IStateMachineResumer resumer = (IStateMachineResumer)inspector.GetResumer(stateMachineType);
        IDAsyncRunnable runnable = resume(resumer, arg, ref this);

        _reader.ExpectToken(JsonTokenType.EndObject);
        _reader.MoveNext();
        _reader.ExpectEnd();

        return new DAsyncLink(parentId, runnable);
    }

    public bool ReadField<TField>(string name, ref TField? value)
    {
        if (_reader.TokenType is not JsonTokenType.PropertyName)
            return false;

        if (_reader.ValueSpan.StartsWith(StateMachineJsonConstants.MarshaledValuePrefixUtf8))
            return ReadMarshaledValue(name, ref value);

        if (!_reader.ValueTextEquals(name))
            return false;

        _reader.MoveNext();
        value = JsonSerializer.Deserialize<TField>(ref _reader, jsonOptions);

        _reader.MoveNext();
        return true;
    }

    internal DAsyncId ReadParentId()
    {
        throw new NotImplementedException();
    }

    internal TypeId ReadTypeId()
    {
        throw new NotImplementedException();
    }

    private bool ReadMarshaledValue<TField>(string name, ref TField? value)
    {
        ReadOnlySpan<char> namePrefix = StateMachineJsonConstants.MarshaledValuePrefix;
        int prefixLength = namePrefix.Length;
        Span<char> finalName = stackalloc char[prefixLength + name.Length];
        namePrefix.CopyTo(finalName);
        name.AsSpan().CopyTo(finalName[prefixLength..]);

        if (!_reader.ValueTextEquals(finalName))
            return false;

        _reader.MoveNext();
        _reader.ExpectToken(JsonTokenType.StartObject);

        _reader.MoveNext();
        if (_reader.TokenType == JsonTokenType.EndObject)
            return ReadNullToken(default, ref value);

        _reader.ExpectToken(JsonTokenType.PropertyName);
        if (_reader.ValueTextEquals("$ref"))
            return ReadReference(ref value);

        string? referenceId = null;
        TypeId typeId = default;

        if (_reader.ValueTextEquals("$id"))
        {
            if (!typeof(TField).IsValueType)
                throw new JsonException($"Unexpected property name '{_reader.GetString()}' while deserializing a d-async token.");

            _reader.MoveNext();
            _reader.ExpectToken(JsonTokenType.String);

            referenceId = _reader.GetString()!;

            _reader.MoveNext();
            _reader.ExpectToken(JsonTokenType.PropertyName);
        }

        if (_reader.ValueTextEquals("typeId"))
        {
            _reader.MoveNext();
            typeId = JsonSerializer.Deserialize<TypeId>(ref _reader, jsonOptions);

            _reader.MoveNext();
            if (_reader.TokenType == JsonTokenType.EndObject)
                return ReadNullToken(typeId, ref value);

            _reader.ExpectToken(JsonTokenType.PropertyName);
        }

        if (!_reader.ValueTextEquals("token"))
            throw new JsonException($"Unexpected property name '{_reader.GetString()}' while deserializing a d-async token.");

        _reader.MoveNext();

#if NET9_0_OR_GREATER
        UnmarshalingAction<TField> action = new(ref _reader, jsonOptions, ref value);
        if (marshaler.TryUnmarshal<TField, UnmarshalingAction<TField>>(typeId, ref action))
        {
            TryAddReference(referenceId, value);
            return true;
        }

        return false;
#else
        if (marshaler.TryUnmarshal<TField>(typeId, out UnmarshalResult result))
        {
            object? token = JsonSerializer.Deserialize(ref _reader, result.TokenType, jsonOptions);
            value = result.Converter.Convert<object?, TField>(token);

            TryAddReference(referenceId, value);
            return true;
        }

        return false;
#endif
    }

    private bool ReadNullToken<TField>(TypeId typeId, ref TField? value)
    {
        _reader.MoveNext();

#if NET9_0_OR_GREATER
        NullTokenUnmarshalingAction<TField> action = new(ref value);
        return marshaler.TryUnmarshal<TField, NullTokenUnmarshalingAction<TField>>(typeId, ref action);
#else
        if (marshaler.TryUnmarshal<TField>(default, out UnmarshalResult result))
        {
            value = result.Converter.Convert<object?, TField>(null);
            return true;
        }

        return false;
#endif
    }

    private bool ReadReference<TField>(ref TField? value)
    {
        _reader.MoveNext();
        _reader.ExpectToken(JsonTokenType.String);

        string referenceId = _reader.GetString()!;
        value = (TField)referenceResolver.ResolveReference(referenceId);

        _reader.MoveNext();
        _reader.ExpectToken(JsonTokenType.EndObject);

        _reader.MoveNext();
        return true;
    }

    private readonly void TryAddReference(string? referenceId, object? value)
    {
        if (referenceId is null)
            return;

        if (value is null)
            throw new InvalidOperationException("A d-async token was unexpectedly unmarshaled as a null reference.");

        referenceResolver.AddReference(referenceId, value);
    }

#if NET9_0_OR_GREATER

    private ref struct NullTokenUnmarshalingAction<TField>(ref TField? value) : IUnmarshalingAction
    {
        private ref TField? _value = ref value;

        public void UnmarshalAs<TConverter>(Type tokenType, ref TConverter converter)
            where TConverter : struct, ITokenConverter
        {
            _value = converter.Convert<object?, TField>(null);
        }

        public void UnmarshalAs(Type tokenType, ITokenConverter converter)
        {
            _value = converter.Convert<object?, TField>(null);
        }
    }

    private ref struct UnmarshalingAction<TField>(
        ref Utf8JsonReader reader,
        JsonSerializerOptions jsonOptions,
        ref TField? value) : IUnmarshalingAction
    {
        private ref ReinterpretMeAsUtf8JsonReader _reader = ref Unsafe.As<Utf8JsonReader, ReinterpretMeAsUtf8JsonReader>(ref reader);
        private ref TField? _value = ref value;

        public void UnmarshalAs<TConverter>(Type tokenType, scoped ref TConverter converter)
            where TConverter : struct, ITokenConverter
        {
            object? token = ReadToken(tokenType);
            _value = converter.Convert<object?, TField>(token);
        }

        public void UnmarshalAs(Type tokenType, ITokenConverter converter)
        {
            object? token = ReadToken(tokenType);
            _value = converter.Convert<object?, TField>(token);
        }

        private object? ReadToken(Type tokenType)
        {
            ref Utf8JsonReader reader = ref Unsafe.As<ReinterpretMeAsUtf8JsonReader, Utf8JsonReader>(ref _reader);
            object? token = JsonSerializer.Deserialize(ref reader, tokenType, jsonOptions);
            return token;
        }

        // The compiler won't let us store a reference to the Utf8JsonReader, since it is a ref struct.
        // This allows us to bypass that check. At the same time, we must make sure that we don't misuse the reference.
        private struct ReinterpretMeAsUtf8JsonReader;
    }

#endif
}
