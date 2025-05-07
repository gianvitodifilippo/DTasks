using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Configuration;

internal interface IDAsyncInfrastructure
{
    IDAsyncTypeResolver TypeResolver { get; }

    IDAsyncHeap Heap { get; }

    IDAsyncSurrogator Surrogator { get; }

    IDAsyncCancellationProvider CancellationProvider { get; }

    IDAsyncSuspensionHandler SuspensionHandler { get; }

    IDAsyncStack GetStack(IDAsyncFlowScope scope);
}
