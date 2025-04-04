using DTasks.Execution;
using DTasks.Marshaling;
using DTasks.Utils;

namespace DTasks.Infrastructure;

public abstract class DAsyncHost : IDAsyncHost
{
    // TODO: Pool flows

    protected abstract ITypeResolver TypeResolver { get; }

    protected virtual IDistributedCancellationProvider CancellationProvider => DefaultDistributedCancellationProvider.Instance;

    protected abstract IDAsyncStateManager CreateStateManager(IDAsyncMarshaler marshaler);

    protected virtual IDAsyncMarshaler CreateMarshaler()
    {
        return NullDAsyncMarshaler.Instance;
    }

    public ValueTask StartAsync(IDAsyncRunnable runnable, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(runnable);

        DAsyncFlow flow = new();
        return flow.StartAsync(this, runnable, cancellationToken);
    }

    public ValueTask ResumeAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        DAsyncFlow flow = new();
        return flow.ResumeAsync(this, id, cancellationToken);
    }

    public ValueTask ResumeAsync<TResult>(DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        DAsyncFlow flow = new();
        return flow.ResumeAsync(this, id, result, cancellationToken);
    }

    public ValueTask ResumeAsync(DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        DAsyncFlow flow = new();
        return flow.ResumeAsync(this, id, exception, cancellationToken);
    }

    public Task CancelAsync(DCancellationId id, CancellationToken cancellationToken = default)
    {
        return CancellationProvider.CancelAsync(id, cancellationToken);
    }

    protected virtual Task OnSucceedAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnSucceedAsync<TResult>(TResult result, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnFailAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnCancelAsync(OperationCanceledException exception, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnYieldAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("The current d-async host does not support yielding.");
    }

    protected virtual Task OnDelayAsync(DAsyncId id, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("The current d-async host does not support delaying.");
    }

    ITypeResolver IDAsyncHost.TypeResolver => TypeResolver;

    IDistributedCancellationProvider IDAsyncHost.CancellationProvider => CancellationProvider;

    IDAsyncMarshaler IDAsyncHost.CreateMarshaler() => CreateMarshaler();

    IDAsyncStateManager IDAsyncHost.CreateStateManager(IDAsyncMarshaler marshaler) => CreateStateManager(marshaler);

    Task IDAsyncHost.OnSucceedAsync(CancellationToken cancellationToken) => OnSucceedAsync(cancellationToken);

    Task IDAsyncHost.OnSucceedAsync<TResult>(TResult result, CancellationToken cancellationToken) => OnSucceedAsync(result, cancellationToken);

    Task IDAsyncHost.OnFailAsync(Exception exception, CancellationToken cancellationToken) => OnFailAsync(exception, cancellationToken);

    Task IDAsyncHost.OnCancelAsync(OperationCanceledException exception, CancellationToken cancellationToken) => OnCancelAsync(exception, cancellationToken);

    Task IDAsyncHost.OnYieldAsync(DAsyncId id, CancellationToken cancellationToken) => OnYieldAsync(id, cancellationToken);

    Task IDAsyncHost.OnDelayAsync(DAsyncId id, TimeSpan delay, CancellationToken cancellationToken) => OnDelayAsync(id, delay, cancellationToken);
}
