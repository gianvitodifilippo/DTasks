using DTasks.Hosting;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace DTasks.Serialization.Json;

internal sealed class DTaskReferenceResolver(IDTaskScope scope, JsonSerializerOptions rootOptions) : ReferenceResolver
{
    private readonly Dictionary<string, object> _idsToReferences = [];
    private readonly Dictionary<object, string> _referencesToIds = [];
    private bool _isSerializing;

    public ReferenceHandler CreateHandler() => new DTaskReferenceHandler(this);

    public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
    {
        _isSerializing = true;
        try
        {
            writer.WriteStartArray();

            foreach ((object reference, string id) in _referencesToIds)
            {
                if (!_idsToReferences.TryGetValue(id, out object? referenceOrToken))
                    continue; // We already wrote this reference, don't write it again.

                writer.WriteStartObject();

                if (ReferenceEquals(reference, referenceOrToken))
                {
                    // This is the first time we encounter this reference. Write it.
                    writer.WriteString("type"u8, reference.GetType().AssemblyQualifiedName);
                    writer.WritePropertyName("value"u8);
                    JsonSerializer.Serialize(writer, reference, options);
                }
                else
                {
                    // This is a token.
                    object token = referenceOrToken;
                    writer.WriteString("id"u8, id);
                    writer.WriteString("type"u8, token.GetType().AssemblyQualifiedName);
                    writer.WritePropertyName("value"u8);
                    JsonSerializer.Serialize(writer, token, rootOptions);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
        finally
        {
            _isSerializing = false;
        }
    }

    public void Deserialize(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        reader.MoveNext();
        reader.ExpectType(JsonTokenType.StartArray);

        while (true)
        {
            reader.MoveNext();
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            reader.ExpectType(JsonTokenType.StartObject);

            bool isToken;
            string? id = null;
            reader.MoveNext();
            reader.ExpectType(JsonTokenType.PropertyName);
            if (isToken = reader.ValueTextEquals("id"u8))
            {
                reader.MoveNext();
                id = reader.GetString();

                reader.MoveNext();
                reader.ExpectType(JsonTokenType.PropertyName);
            }

            if (!reader.ValueTextEquals("type"u8))
                throw new JsonException("Invalid heap json.");

            reader.MoveNext();
            string? typeId = reader.GetString();
            if (typeId is null)
                throw new JsonException("Invalid heap json.");

            Type type = Type.GetType(typeId, throwOnError: true)!;

            reader.MoveNext();
            reader.ExpectType(JsonTokenType.PropertyName);
            if (!reader.ValueTextEquals("value"u8))
                throw new JsonException("Invalid heap json.");

            reader.MoveNext();
            if (isToken)
            {
                object token = JsonSerializer.Deserialize(ref reader, type, rootOptions) ?? throw new JsonException("Invalid heap json.");
                if (!scope.TryGetReference(token, out object? reference))
                    throw new JsonException("Invalid json heap.");

                _idsToReferences[id!] = token;
                _referencesToIds[reference] = id!;
            }
            else
            {
                _ = JsonSerializer.Deserialize(ref reader, type, options) ?? throw new JsonException("Invalid heap json.");
            }

            reader.MoveNext();
            reader.ExpectType(JsonTokenType.EndObject);
        }

        reader.ExpectEnd();
    }

    public override string GetReference(object value, out bool alreadyExists)
    {
        Debug.Assert(value is not string, "Strings should be serialized as values.");

        if (_referencesToIds.TryGetValue(value, out string? referenceId))
        {
            alreadyExists = !_isSerializing || !_idsToReferences.Remove(referenceId);
            return referenceId;
        }

        Debug.Assert(!_isSerializing, "We should have scanned all references before serializing the heap.");

        referenceId = _referencesToIds.Count.ToString();
        _referencesToIds.Add(value, referenceId);

        // TODO: Verify if the following can actually work.
        // When calling 'WriteReference', the JsonSerializer will need a reference resolver that
        // preserves references, and there are two options:
        // 1. Use the current instance - the problem is that the serialization happens while iterating
        // over the _referencesToIds dictionary, therefore we can't modify it.
        // 2. Use a new instance - the problem is that we allocate more memory and we don't use
        // the references stored until that point.
        // A possible solution is to trasverse the object graph here, provided that we know the reference
        // will be serialized as it is, i.e., it will be not surrogated by a token provided by the host.
        // We could call scope.TryGetReferenceToken (and discard the token): if it returns false,
        // proceed with the graph trasversal. The downside is that upon serialization, we will have to
        // call that method again, unless we store the result somewhere. This might potentially hurt
        // performances since the host itself, might need to trasverse the object graph again or allocate
        // itself some memory to cache the result. Therefore, to avoid allocating a new dictionary within this
        // instance to map references to token or have the host do the same thing, let's instead use _idsToReferences:
        // the first time the d-async flow is suspended (DTaskHost.SuspendAsync), it will start empty and won't be
        // used for anything else. The rest of the time, either _idsToReferences already contains the token
        // associated with the reference (coming from deserialization) or we get it from the host and store into
        // it ourselves. This way, we can use _idsToReferences within 'WriteReferences' to tell whether the value
        // is a token or not.

        if (_idsToReferences.TryGetValue(referenceId, out object? referenceOrToken))
        {
            // This value was previously encountered
            if (ReferenceEquals(referenceOrToken, value))
            {
                // Not a token. Trasverse the graph.
                TrasverseGraph(value);
            }
            else
            {
                // Token. We don't need to do anything.
            }
        }
        else
        {
            // This value was created during last execution
            if (scope.TryGetReferenceToken(value, out object? token))
            {
                // A new token. Map it to the current reference id.
                _idsToReferences[referenceId] = token;
            }
            else
            {
                // Not a token. Add the reference to the dictionary and trasverse the graph.
                _idsToReferences[referenceId] = value;
                TrasverseGraph(value);
            }
        }

        alreadyExists = true;
        return referenceId;
    }

    public override void AddReference(string referenceId, object value)
    {
        _idsToReferences[referenceId] = value;
        _referencesToIds[value] = referenceId;
    }

    public override object ResolveReference(string referenceId)
    {
        object referenceOrToken = _idsToReferences[referenceId];

        // We don't replace the reference with the token here: this would break
        // the 'GetReference' method and the implementation of 'TryGetReference' should be
        // cheaper than 'TryGetReferenceToken' and shouldn't require caching.
        return scope.TryGetReference(referenceOrToken, out object? reference)
            ? reference
            : referenceOrToken;
    }

    //private bool IsProvidedByHost(string id, object reference, [NotNullWhen(true)] out object? token)
    //{
    //     If the reference is provided by the host, we should have stored the token
    //     it is associated with inside _idsToReferences.

    //    return
    //        _idsToReferences.TryGetValue(id, out token) &&
    //        !ReferenceEquals(reference, token);
    //}

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

            _ = GetReference(propertyValue, out _);
        }
    }

    private sealed class DTaskReferenceHandler(DTaskReferenceResolver resolver) : ReferenceHandler
    {
        public override ReferenceResolver CreateResolver() => resolver;
    }
}
