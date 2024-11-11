using DTasks.Marshaling;

namespace DTasks.Hosting;

internal partial class DAsyncFlow : IDAsyncMarshaler
{
    bool IDAsyncMarshaler.TryMarshal<T, TAction>(string fieldName, in T value, scoped ref TAction action)
    {
        return _marshaler.TryMarshal(fieldName, in value, ref action);
    }

    bool IDAsyncMarshaler.TryUnmarshal<T, TAction>(string fieldName, TypeId typeId, scoped ref TAction action)
    {
        return _marshaler.TryUnmarshal<T, TAction>(fieldName, typeId, ref action);
    }
}
