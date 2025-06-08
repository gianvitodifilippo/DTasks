using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncSurrogator
{
    bool TrySurrogate<T, TMarshaller>(in T value, scoped ref TMarshaller marshaller)
        where TMarshaller : IMarshaller
#if NET9_0_OR_GREATER
        , allows ref struct;
#else
        ;
#endif

    bool TryRestore<T, TUnmarshaller>(TypeId typeId, scoped ref TUnmarshaller unmarshaller, [MaybeNullWhen(false)] out T value)
        where TUnmarshaller : IUnmarshaller
#if NET9_0_OR_GREATER
        , allows ref struct;
#else
        ;
#endif
}
