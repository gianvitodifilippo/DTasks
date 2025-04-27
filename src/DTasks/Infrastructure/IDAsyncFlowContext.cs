using System.ComponentModel;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncFlowContext
{
    IDAsyncStack Stack { get; }

    IDAsyncHeap Heap { get; }

    IDAsyncSurrogator Surrogator { get; }

    IDAsyncCancellationProvider CancellationProvider { get; }

    IDAsyncSuspensionHandler SuspensionHandler { get; }
}