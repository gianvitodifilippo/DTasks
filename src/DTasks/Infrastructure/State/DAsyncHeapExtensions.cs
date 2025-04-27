#if NET9_0_OR_GREATER

using System.ComponentModel;
using DTasks.Utils;

namespace DTasks.Infrastructure.State;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class DAsyncHeapExtensions
{
    public static Task SaveAsync<TValue>(this IDAsyncHeap heap, ReadOnlySpan<char> key, TValue value, CancellationToken cancellationToken = default)
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
    
    public static Task SaveAsync<TValue>(this IDAsyncHeap heap, ReadOnlySpan<byte> key, TValue value, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(heap);
        
        if (heap is IDAsyncHeapSupportsRefStructKey supportsRefStructKey && supportsRefStructKey.SupportsKeyType(typeof(ReadOnlySpan<byte>)))
            return supportsRefStructKey.SaveAsync(key, value, cancellationToken);

        string stringKey = key.ToString();
        return heap.SaveAsync(stringKey, value, cancellationToken);
    }
    
    public static Task<Option<TValue>> LoadAsync<TValue>(this IDAsyncHeap heap, ReadOnlySpan<byte> key, CancellationToken cancellationToken = default)
    {
        if (heap is IDAsyncHeapSupportsRefStructKey supportsRefStructKey && supportsRefStructKey.SupportsKeyType(typeof(ReadOnlySpan<byte>)))
            return supportsRefStructKey.LoadAsync<ReadOnlySpan<byte>, TValue>(key, cancellationToken);

        string stringKey = key.ToString();
        return heap.LoadAsync<string, TValue>(stringKey, cancellationToken);
    }
    
    public static Task DeleteAsync(this IDAsyncHeap heap, ReadOnlySpan<byte> key, CancellationToken cancellationToken = default)
    {
        if (heap is IDAsyncHeapSupportsRefStructKey supportsRefStructKey && supportsRefStructKey.SupportsKeyType(typeof(ReadOnlySpan<byte>)))
            return supportsRefStructKey.DeleteAsync(key, cancellationToken);

        string stringKey = key.ToString();
        return heap.DeleteAsync(stringKey, cancellationToken);
    }
}

#endif
