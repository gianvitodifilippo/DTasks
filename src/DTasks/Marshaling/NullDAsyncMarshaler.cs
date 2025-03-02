namespace DTasks.Marshaling;

internal sealed class NullDAsyncMarshaler : IDAsyncMarshaler
{
    public static readonly NullDAsyncMarshaler Instance = new();

    private NullDAsyncMarshaler()
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
