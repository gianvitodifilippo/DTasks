using DTasks.Execution;
using DTasks.Marshaling;

namespace DTasks.Infrastructure;

internal interface IDAsyncHost
{
    ITypeResolver TypeResolver { get; }

    IDAsyncCancellationProvider CancellationProvider { get; }

    IDAsyncMarshaler CreateMarshaler();

    IDAsyncStateManager CreateStateManager(IDAsyncMarshaler marshaler);

    Task OnSucceedAsync(CancellationToken cancellationToken = default);

    Task OnSucceedAsync<TResult>(TResult result, CancellationToken cancellationToken = default);

    Task OnFailAsync(Exception exception, CancellationToken cancellationToken = default);

    Task OnCancelAsync(OperationCanceledException exception, CancellationToken cancellationToken = default);

    Task OnYieldAsync(DAsyncId id, CancellationToken cancellationToken = default);

    Task OnDelayAsync(DAsyncId id, TimeSpan delay, CancellationToken cancellationToken = default);
}
