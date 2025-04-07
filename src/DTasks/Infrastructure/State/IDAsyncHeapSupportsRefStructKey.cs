#if NET9_0_OR_GREATER

using System.ComponentModel;
using DTasks.Infrastructure.Marshaling;
using DTasks.Utils;

namespace DTasks.Infrastructure.State;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncHeapSupportsRefStructKey
{
    bool SupportsKeyType(Type keyType);

    Task SaveAsync<TKey, TValue>(TKey key, TypedInstance<TValue> value, CancellationToken cancellationToken = default)
        where TKey : notnull, allows ref struct;
    
    Task<Option<TValue>> LoadAsync<TKey, TValue>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull, allows ref struct;
    
    Task DeleteAsync<TKey>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull, allows ref struct;
}

#endif
