using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncSurrogator
{
    bool TrySurrogate<T, TAction>(in T value, scoped ref TAction action)
        where TAction : struct, ISurrogationAction
#if NET9_0_OR_GREATER
        , allows ref struct;
#else
        ;
#endif

    bool TryRestore<T, TAction>(TypeId typeId, scoped ref TAction action)
        where TAction : struct, IRestorationAction
#if NET9_0_OR_GREATER
        , allows ref struct;
#else
        ;
#endif
}
