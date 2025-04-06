#if NET9_0_OR_GREATER

using System.ComponentModel;
using DTasks.Utils;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncHeapSupportsRefStructKey
{
    bool SupportsKeyType(Type keyType);

    Task SaveAsync<TKey, TValue>(TKey key, TValue value, CancellationToken cancellationToken = default)
        where TKey : notnull, allows ref struct;
    
    Task<Option<TValue>> LoadAsync<TKey, TValue>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull, allows ref struct;
    
    Task DeleteAsync<TKey>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull, allows ref struct;
}

#endif
