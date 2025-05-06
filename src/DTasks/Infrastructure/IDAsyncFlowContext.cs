using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncFlowContext
{
    bool TryGetProperty<TProperty>(DAsyncFlowPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value);
    
    // IDAsyncStack Stack { get; }
    //
    // IDAsyncHeap Heap { get; }
    //
    // IDAsyncSurrogator Surrogator { get; }
    //
    // IDAsyncCancellationProvider CancellationProvider { get; }
    //
    // IDAsyncSuspensionHandler SuspensionHandler { get; }
}