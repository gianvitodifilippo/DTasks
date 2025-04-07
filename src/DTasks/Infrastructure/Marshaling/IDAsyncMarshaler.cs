using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncMarshaler
{
    bool TryMarshal<T, TAction>(in T value, scoped ref TAction action)
        where TAction : struct, IMarshalingAction
#if NET9_0_OR_GREATER
        , allows ref struct;
#else
        ;
#endif

    bool TryUnmarshal<T, TAction>(TypeId typeId, scoped ref TAction action)
        where TAction : struct, IUnmarshalingAction
#if NET9_0_OR_GREATER
        , allows ref struct;
#else
        ;
#endif
}
