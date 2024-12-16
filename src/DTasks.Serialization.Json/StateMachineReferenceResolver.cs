using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DTasks.Serialization.Json;

internal class StateMachineReferenceResolver : ReferenceResolver
{
    private uint _referenceCount;
    private Dictionary<string, object>? _referenceIdToObjectMap;
    private Dictionary<object, string>? _objectToReferenceIdMap;

    public void InitForWriting()
    {
        _objectToReferenceIdMap ??= new Dictionary<object, string>(ReferenceEqualityComparer.Instance);
    }

    public void InitForReading()
    {
        _referenceIdToObjectMap ??= new Dictionary<string, object>();
    }

    public override void AddReference(string referenceId, object value)
    {
        Debug.Assert(_referenceIdToObjectMap != null);

        if (!_referenceIdToObjectMap.TryAdd(referenceId, value))
            throw new JsonException($"The value of the '$id' metadata property '{referenceId}' conflicts with an existing identifier.");
    }

    public override string GetReference(object value, out bool alreadyExists)
    {
        Debug.Assert(_objectToReferenceIdMap != null);

        if (_objectToReferenceIdMap.TryGetValue(value, out string? referenceId))
        {
            alreadyExists = true;
        }
        else
        {
            _referenceCount++;
            referenceId = _referenceCount.ToString();
            _objectToReferenceIdMap.Add(value, referenceId);
            alreadyExists = false;
        }

        return referenceId;
    }

    public override object ResolveReference(string referenceId)
    {
        Debug.Assert(_referenceIdToObjectMap != null);

        if (!_referenceIdToObjectMap.TryGetValue(referenceId, out object? value))
            throw new JsonException($"Reference '{referenceId}' was not found.");

        return value;
    }

    public void Clear()
    {
        _referenceIdToObjectMap?.Clear();
        _objectToReferenceIdMap?.Clear();
        _referenceCount = 0;
    }
}
