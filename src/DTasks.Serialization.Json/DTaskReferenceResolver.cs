using DTasks.Hosting;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace DTasks.Serialization.Json;

internal sealed class DTaskReferenceResolver(IDTaskScope scope, JsonSerializerOptions rootOptions) : ReferenceResolver
{
    private static ReadOnlySpan<byte> StackCountKeyUtf8 => "s_count"u8;
    private static ReadOnlySpan<byte> HeapKeyUtf8 => "heap"u8;
    private static ReadOnlySpan<byte> TypeKeyUtf8 => "type"u8;
    private static ReadOnlySpan<byte> IdKeyUtf8 => "id"u8;
    private static ReadOnlySpan<byte> ValueKeyUtf8 => "value"u8;

    private readonly Dictionary<string, object> _idsToReferences = [];
    private readonly Dictionary<object, string> _referencesToIds = [];
    private bool _isSerializing;

    public ReferenceHandler CreateHandler() => new DTaskReferenceHandler(this);

    public void Serialize(ref JsonFlowHeap heap)
    {
        Utf8JsonWriter writer = heap.Writer;
        JsonSerializerOptions options = heap.Options;

        _isSerializing = true;
        try
        {
            writer.WriteStartObject();

            writer.WriteNumber(StackCountKeyUtf8, heap.StackCount);

            writer.WritePropertyName(HeapKeyUtf8);

            writer.WriteStartArray();

            foreach ((object reference, string id) in _referencesToIds)
            {
                if (!_idsToReferences.TryGetValue(id, out _))
                    continue; // We already wrote this reference, don't write it again

                writer.WriteStartObject();

                if (scope.TryGetReferenceToken(reference, out object? token))
                {
                    writer.WriteString(IdKeyUtf8, id);
                    writer.WriteString(TypeKeyUtf8, token.GetType().AssemblyQualifiedName);
                    writer.WritePropertyName(ValueKeyUtf8);
                    JsonSerializer.Serialize(writer, token, rootOptions);
                }
                else
                {
                    writer.WriteString(TypeKeyUtf8, reference.GetType().AssemblyQualifiedName);
                    writer.WritePropertyName(ValueKeyUtf8);
                    JsonSerializer.Serialize(writer, reference, options);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteEndObject();
        }
        finally
        {
            _isSerializing = false;
        }
    }

    public void Deserialize(ref Utf8JsonReader reader, ref JsonFlowHeap heap, JsonSerializerOptions options)
    {
        reader.MoveNext();
        reader.ExpectType(JsonTokenType.StartObject);

        reader.MoveNext();
        reader.ExpectType(JsonTokenType.PropertyName);
        if (!reader.ValueTextEquals(StackCountKeyUtf8))
            throw InvalidJsonHeap();

        reader.MoveNext();
        reader.ExpectType(JsonTokenType.Number);
        heap.StackCount = reader.GetUInt32();

        reader.MoveNext();
        reader.ExpectType(JsonTokenType.PropertyName);
        if (!reader.ValueTextEquals(HeapKeyUtf8))
            throw InvalidJsonHeap();

        reader.MoveNext();
        reader.ExpectType(JsonTokenType.StartArray);

        while (true)
        {
            reader.MoveNext();
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            reader.ExpectType(JsonTokenType.StartObject);

            reader.MoveNext();
            reader.ExpectType(JsonTokenType.PropertyName);
            if (reader.ValueTextEquals(IdKeyUtf8))
            {
                reader.MoveNext();
                string referenceId = reader.GetString() ?? throw InvalidJsonHeap();

                reader.MoveNext();
                reader.ExpectType(JsonTokenType.PropertyName);

                Type type = ReadType(ref reader);
                MoveToValue(ref reader);

                object token = JsonSerializer.Deserialize(ref reader, type, rootOptions) ?? throw InvalidJsonHeap();
                if (!scope.TryGetReference(token, out object? reference))
                    throw InvalidJsonHeap();

                _idsToReferences[referenceId] = reference;
                _referencesToIds[reference] = referenceId;
            }
            else
            {
                Type type = ReadType(ref reader);
                MoveToValue(ref reader);

                _ = JsonSerializer.Deserialize(ref reader, type, options) ?? throw InvalidJsonHeap();
            }

            reader.MoveNext();
            reader.ExpectType(JsonTokenType.EndObject);
        }

        reader.MoveNext();
        reader.ExpectType(JsonTokenType.EndObject);

        reader.ExpectEnd();

        static Type ReadType(ref Utf8JsonReader reader)
        {
            if (!reader.ValueTextEquals(TypeKeyUtf8))
                throw InvalidJsonHeap();

            reader.MoveNext();
            string? typeId = reader.GetString();
            if (typeId is null)
                throw InvalidJsonHeap();

            return Type.GetType(typeId, throwOnError: true)!;
        }

        static void MoveToValue(ref Utf8JsonReader reader)
        {
            reader.MoveNext();
            reader.ExpectType(JsonTokenType.PropertyName);
            if (!reader.ValueTextEquals(ValueKeyUtf8))
                throw InvalidJsonHeap();

            reader.MoveNext();
        }
    }

    public string GetReference(object value)
    {
        string reference = GetReference(value, out bool alreadyExists);

        Debug.Assert(alreadyExists, "All references should be stored on the heap."); // i.e., do not call this method during serialization
        return reference;
    }

    public override string GetReference(object value, out bool alreadyExists)
    {
        Debug.Assert(value is not string, "Strings should be serialized as values.");

        if (_referencesToIds.TryGetValue(value, out string? referenceId))
        {
            // When serializing, remove the reference from the dictionary to indicate that it was already written
            alreadyExists = !_isSerializing || !_idsToReferences.Remove(referenceId);
            return referenceId;
        }

        Debug.Assert(!_isSerializing, "We should have scanned all references before serializing the heap.");

        referenceId = _referencesToIds.Count.ToString();
        _idsToReferences.Add(referenceId, value);
        _referencesToIds.Add(value, referenceId);

        if (!scope.TryGetReferenceToken(value, out _))
        {
            // Trasverse the object graph here because we won't be able to do it during serialization
            TrasverseGraph(value);
        }

        alreadyExists = !_isSerializing; // i.e., true
        return referenceId;
    }

    public override void AddReference(string referenceId, object value)
    {
        Debug.Assert(!_isSerializing, $"'{nameof(AddReference)}' should not be called during serialization.");

        _idsToReferences[referenceId] = value;
        _referencesToIds[value] = referenceId;
    }

    public override object ResolveReference(string referenceId)
    {
        Debug.Assert(!_isSerializing, $"'{nameof(ResolveReference)}' should not be called during serialization.");

        return _idsToReferences[referenceId];
    }

    private void TrasverseGraph(object value)
    {
        Debug.Assert(!_isSerializing, "Graph trasversal should have happened before heap serialization.");

        JsonTypeInfo typeInfo = rootOptions.GetTypeInfo(value.GetType());
        foreach (JsonPropertyInfo property in typeInfo.Properties)
        {
            if (property.PropertyType.IsValueType || property.PropertyType == typeof(string))
                continue;

            if (property.Get is not Func<object, object?> getter)
                continue;

            if (getter(value) is not object propertyValue)
                continue;

            _ = GetReference(propertyValue);
        }
    }

    private static JsonException InvalidJsonHeap() => new JsonException("Invalid json heap.");

    private sealed class DTaskReferenceHandler(DTaskReferenceResolver resolver) : ReferenceHandler
    {
        public override ReferenceResolver CreateResolver() => resolver;
    }
}
