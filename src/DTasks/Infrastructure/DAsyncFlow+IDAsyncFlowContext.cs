using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncFlowContext
{
    IDAsyncStack IDAsyncFlowContext.Stack => Stack;

    IDAsyncHeap IDAsyncFlowContext.Heap => Heap;

    IDAsyncSurrogator IDAsyncFlowContext.Surrogator => this;

    IDAsyncCancellationProvider IDAsyncFlowContext.CancellationProvider => CancellationProvider;

    IDAsyncSuspensionHandler IDAsyncFlowContext.SuspensionHandler => SuspensionHandler;
}