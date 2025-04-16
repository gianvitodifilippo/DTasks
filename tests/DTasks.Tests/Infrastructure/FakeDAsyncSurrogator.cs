using DTasks.Infrastructure.Marshaling;

namespace DTasks.Infrastructure;

internal class FakeDAsyncSurrogator : IDAsyncSurrogator
{
    public bool TrySurrogate<T, TAction>(in T value, scoped ref TAction action)
        where TAction : struct, ISurrogationAction
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return false;
    }

    public bool TryRestore<T, TAction>(TypeId typeId, scoped ref TAction action)
        where TAction : struct, IRestorationAction
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return false;
    }
}
