using DTasks.Marshaling;

namespace DTasks.Hosting;

internal class FakeDAsyncMarshaler : IDAsyncMarshaler
{
    public bool TryMarshal<T, TAction>(in T value, scoped ref TAction action)
        where TAction : struct, IMarshalingAction, allows ref struct
    {
        return false;
    }

    public bool TryUnmarshal<T, TAction>(TypeId typeId, scoped ref TAction action)
        where TAction : struct, IUnmarshalingAction, allows ref struct
    {
        return false;
    }
}
