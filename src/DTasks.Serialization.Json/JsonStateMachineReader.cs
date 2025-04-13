using DTasks.Infrastructure;
using DTasks.Inspection;
using System.Text.Json;
using System.Text.Json.Serialization;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
#if NET9_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace DTasks.Serialization.Json;

internal ref struct JsonStateMachineReader(
    ReadOnlySpan<byte> bytes,
    JsonSerializerOptions jsonOptions,
    ReferenceResolver referenceResolver,
    IDAsyncSurrogator surrogator)
{
    public delegate IDAsyncRunnable ResumeAction<in T>(IStateMachineResumer resumer, T arg, ref JsonStateMachineReader reader);

    private Utf8JsonReader _reader = new(bytes);

    public DAsyncLink DeserializeStateMachine<T>(IStateMachineInspector inspector, IDAsyncTypeResolver typeResolver, T arg, ResumeAction<T> resume)
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

        _reader.MoveNext();
        IDAsyncRunnable runnable = resume(resumer, arg, ref this);

        _reader.ExpectToken(JsonTokenType.EndObject);
        _reader.ExpectEnd();

        return new DAsyncLink(parentId, runnable);
    }

    public bool ReadField<TField>(string name, ref TField? value)
    {
        if (_reader.TokenType is not JsonTokenType.PropertyName)
            return false;

        if (_reader.ValueSpan.StartsWith(StateMachineJsonConstants.SurrogatedValuePrefixUtf8))
            return ReadSurrogatedValue(name, ref value);

        if (!_reader.ValueTextEquals(name))
            return false;

        _reader.MoveNext();
        value = JsonSerializer.Deserialize<TField>(ref _reader, jsonOptions);

        _reader.MoveNext();
        return true;
    }

    private bool ReadSurrogatedValue<TField>(string name, ref TField? value)
    {
        ReadOnlySpan<char> namePrefix = StateMachineJsonConstants.SurrogatedValuePrefix;
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
            if (typeof(TField).IsValueType)
                throw new JsonException($"Unexpected property name '{_reader.GetString()}' while deserializing a d-async surrogate.");

            _reader.MoveNext();
            _reader.ExpectToken(JsonTokenType.String);

            referenceId = _reader.GetString()!;

            _reader.MoveNext();
            _reader.ExpectToken(JsonTokenType.PropertyName);
        }

        if (_reader.ValueTextEquals("typeId"))
        {
            _reader.MoveNext();
            typeId = _reader.ReadTypeId();

            _reader.MoveNext();
            if (_reader.TokenType == JsonTokenType.EndObject)
                return ReadNullToken(typeId, ref value);

            _reader.ExpectToken(JsonTokenType.PropertyName);
        }

        if (!_reader.ValueTextEquals("surrogate"))
            throw new JsonException($"Unexpected property name '{_reader.GetString()}' while deserializing a d-async surrogate.");

        _reader.MoveNext();

#if NET9_0_OR_GREATER
        RestorationAction<TField> action = new(ref _reader, jsonOptions, ref value);
        if (surrogator.TryRestore<TField, RestorationAction<TField>>(typeId, ref action))
        {
            _reader.MoveNext();
            TryAddReference(referenceId, value);
            return true;
        }

        _reader.MoveNext();
        return false;
#else
        if (surrogator.TryRestore<TField>(typeId, out RestorationResult result))
        {
            object? surrogate = JsonSerializer.Deserialize(ref _reader, result.SurrogateType, jsonOptions);
            _reader.MoveNext();

            value = result.Converter.Convert<object?, TField>(surrogate);
            TryAddReference(referenceId, value);

            _reader.MoveNext();
            return true;
        }

        _reader.MoveNext();
        return false;
#endif
    }

    private bool ReadNullToken<TField>(TypeId typeId, ref TField? value)
    {
        _reader.MoveNext();

#if NET9_0_OR_GREATER
        NullSurrogateRestorationAction<TField> action = new(ref value);
        return surrogator.TryRestore<TField, NullSurrogateRestorationAction<TField>>(typeId, ref action);
#else
        if (!surrogator.TryRestore<TField>(default, out RestorationResult result))
            return false;

        value = result.Converter.Convert<object?, TField>(null);
        return true;
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
            throw new InvalidOperationException("A d-async surrogate was unexpectedly restored as a null reference.");

        referenceResolver.AddReference(referenceId, value);
    }

#if NET9_0_OR_GREATER

    private ref struct NullSurrogateRestorationAction<TField>(ref TField? value) : IRestorationAction
    {
        private ref TField? _value = ref value;

        public void RestoreAs<TConverter>(Type surrogateType, ref TConverter converter)
            where TConverter : struct, ISurrogateConverter
        {
            _value = converter.Convert<object?, TField>(null);
        }

        public void RestoreAs(Type surrogateType, ISurrogateConverter converter)
        {
            _value = converter.Convert<object?, TField>(null);
        }
    }

    private ref struct RestorationAction<TField>(
        ref Utf8JsonReader reader,
        JsonSerializerOptions jsonOptions,
        ref TField? value) : IRestorationAction
    {
        private ref ReinterpretMeAsUtf8JsonReader _reader = ref Unsafe.As<Utf8JsonReader, ReinterpretMeAsUtf8JsonReader>(ref reader);
        private ref TField? _value = ref value;

        public void RestoreAs<TConverter>(Type surrogateType, scoped ref TConverter converter)
            where TConverter : struct, ISurrogateConverter
        {
            object? surrogate = ReadSurrogate(surrogateType);
            _value = converter.Convert<object?, TField>(surrogate);
        }

        public void RestoreAs(Type surrogateType, ISurrogateConverter converter)
        {
            object? surrogate = ReadSurrogate(surrogateType);
            _value = converter.Convert<object?, TField>(surrogate);
        }

        private object? ReadSurrogate(Type surrogateType)
        {
            ref Utf8JsonReader reader = ref Unsafe.As<ReinterpretMeAsUtf8JsonReader, Utf8JsonReader>(ref _reader);
            object? surrogate = JsonSerializer.Deserialize(ref reader, surrogateType, jsonOptions);
            reader.MoveNext();
            return surrogate;
        }

        // The compiler won't let us store a reference to the Utf8JsonReader, since it is a ref struct.
        // This allows us to bypass that check. At the same time, we must make sure that we don't misuse the reference.
        private struct ReinterpretMeAsUtf8JsonReader;
    }

#endif
}
