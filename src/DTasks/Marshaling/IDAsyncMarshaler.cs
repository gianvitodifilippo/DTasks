using System.ComponentModel;

namespace DTasks.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncMarshaler
{
    bool TryMarshal<T, TAction>(string fieldName, in T value, scoped ref TAction action)
        where TAction : struct, IMarshalingAction
#if NET9_0_OR_GREATER
        , allows ref struct;
#else
        ;
#endif

    bool TryUnmarshal<T, TAction>(string fieldName, TypeId typeId, scoped ref TAction action)
        where TAction : struct, IUnmarshalingAction
#if NET9_0_OR_GREATER
        , allows ref struct;
#else
        ;
#endif
}
