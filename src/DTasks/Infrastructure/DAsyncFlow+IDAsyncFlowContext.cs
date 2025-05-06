using System.Diagnostics.CodeAnalysis;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncFlowContext
{
    bool IDAsyncFlowContext.TryGetProperty<TProperty>(DAsyncFlowPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value)
    {
        return TryGetProperty(key, out value);
    }
    // IDAsyncStack IDAsyncFlowContext.Stack => Stack;
    //
    // IDAsyncHeap IDAsyncFlowContext.Heap => Heap;
    //
    // IDAsyncSurrogator IDAsyncFlowContext.Surrogator => this;
    //
    // IDAsyncCancellationProvider IDAsyncFlowContext.CancellationProvider => CancellationProvider;
    //
    // IDAsyncSuspensionHandler IDAsyncFlowContext.SuspensionHandler => SuspensionHandler;
}