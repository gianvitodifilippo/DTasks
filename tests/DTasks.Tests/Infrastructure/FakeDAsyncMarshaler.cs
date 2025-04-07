namespace DTasks.Infrastructure;

internal class FakeDAsyncMarshaler : IDAsyncMarshaler
{
    public bool TryMarshal<T, TAction>(in T value, scoped ref TAction action)
        where TAction : struct, IMarshalingAction
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return false;
    }

    public bool TryUnmarshal<T, TAction>(TypeId typeId, scoped ref TAction action)
        where TAction : struct, IUnmarshalingAction
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return false;
    }
}
