namespace DTasks.Marshaling;

internal sealed class DefaultDAsyncMarshaler : IDAsyncMarshaler
{
    public static readonly DefaultDAsyncMarshaler Instance = new();

    private DefaultDAsyncMarshaler()
    {
    }

    bool IDAsyncMarshaler.TryMarshal<T, TAction>(in T value, scoped ref TAction action)
    {
        return false;
    }

    bool IDAsyncMarshaler.TryUnmarshal<T, TAction>(TypeId typeId, scoped ref TAction action)
    {
        return false;
    }
}
