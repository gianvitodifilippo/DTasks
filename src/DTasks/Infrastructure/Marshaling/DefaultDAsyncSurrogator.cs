namespace DTasks.Infrastructure.Marshaling;

internal sealed class DefaultDAsyncSurrogator : IDAsyncSurrogator
{
    public static readonly DefaultDAsyncSurrogator Instance = new();

    private DefaultDAsyncSurrogator()
    {
    }

    bool IDAsyncSurrogator.TrySurrogate<T, TAction>(in T value, scoped ref TAction action)
    {
        return false;
    }

    bool IDAsyncSurrogator.TryRestore<T, TAction>(TypeId typeId, scoped ref TAction action)
    {
        return false;
    }
}
