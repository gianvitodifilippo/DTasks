using DTasks.Marshaling;
using System.Text.Json;

namespace DTasks.Serialization.Json;

internal ref struct JsonStateMachineReader(ReadOnlySpan<byte> bytes, IDAsyncMarshaler marshaler)
{
    private Utf8JsonReader _reader = new(bytes);

    public bool ReadField<TField>(string fieldName, ref TField? value)
    {
        throw new NotImplementedException();
    }
}
