#if NET9_0_OR_GREATER

using System.ComponentModel;
using DTasks.Infrastructure.Marshaling;
using DTasks.Utils;

namespace DTasks.Infrastructure.State;

[EditorBrowsable(EditorBrowsableState.Never)]
public static partial class DAsyncHeapExtensions
{
    public static Task SaveAsync<TValue>(this IDAsyncHeap heap, ReadOnlySpan<char> key, TypedInstance<TValue> value, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(heap);
        
        if (heap is IDAsyncHeapSupportsRefStructKey supportsRefStructKey && supportsRefStructKey.SupportsKeyType(typeof(ReadOnlySpan<char>)))
            return supportsRefStructKey.SaveAsync(key, value, cancellationToken);

        string stringKey = key.ToString();
        return heap.SaveAsync(stringKey, value, cancellationToken);
    }

    public static Task<Option<TValue>> LoadAsync<TValue>(this IDAsyncHeap heap, ReadOnlySpan<char> key, CancellationToken cancellationToken = default)
    {
        if (heap is IDAsyncHeapSupportsRefStructKey supportsRefStructKey && supportsRefStructKey.SupportsKeyType(typeof(ReadOnlySpan<char>)))
            return supportsRefStructKey.LoadAsync<ReadOnlySpan<char>, TValue>(key, cancellationToken);

        string stringKey = key.ToString();
        return heap.LoadAsync<string, TValue>(stringKey, cancellationToken);
    }
    
    public static Task DeleteAsync(this IDAsyncHeap heap, ReadOnlySpan<char> key, CancellationToken cancellationToken = default)
    {
        if (heap is IDAsyncHeapSupportsRefStructKey supportsRefStructKey && supportsRefStructKey.SupportsKeyType(typeof(ReadOnlySpan<char>)))
            return supportsRefStructKey.DeleteAsync(key, cancellationToken);

        string stringKey = key.ToString();
        return heap.DeleteAsync(stringKey, cancellationToken);
    }
}

#endif
