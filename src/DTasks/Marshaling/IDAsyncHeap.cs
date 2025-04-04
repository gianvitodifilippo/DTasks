using DTasks.Utils;

namespace DTasks.Marshaling;

public interface IDAsyncHeap
{
    Task SaveAsync<TKey, TValue>(TKey key, TValue value, CancellationToken cancellationToken = default)
        where TKey : notnull, IEquatable<TKey>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        ;
    
    Task<Option<TValue>> LoadAsync<TKey, TValue>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull, IEquatable<TKey>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        ;
    
    Task DeleteAsync<TKey>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull, IEquatable<TKey>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        ;
}