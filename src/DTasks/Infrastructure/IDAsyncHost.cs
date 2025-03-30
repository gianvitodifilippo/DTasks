using DTasks.Marshaling;

namespace DTasks.Infrastructure;

internal interface IDAsyncHost
{
    ITypeResolver TypeResolver { get; }

    IDAsyncMarshaler CreateMarshaler();

    IDAsyncStateManager CreateStateManager(IDAsyncMarshaler marshaler);

    Task SucceedAsync(CancellationToken cancellationToken = default);

    Task SucceedAsync<TResult>(TResult result, CancellationToken cancellationToken = default);

    Task FailAsync(Exception exception, CancellationToken cancellationToken = default);

    Task YieldAsync(DAsyncId id, CancellationToken cancellationToken = default);
    
    Task DelayAsync(DAsyncId id, TimeSpan delay, CancellationToken cancellationToken = default);
}
