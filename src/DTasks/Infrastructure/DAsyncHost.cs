using DTasks.Infrastructure;
using DTasks.Marshaling;
using DTasks.Utils;

namespace DTasks.Infrastructure;

public abstract class DAsyncHost : IDAsyncHost
{
    // TODO: Pool flows

    protected abstract ITypeResolver TypeResolver { get; }

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

    protected virtual Task SucceedAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected virtual Task SucceedAsync<TResult>(TResult result, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected virtual Task FailAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    protected virtual Task YieldAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("The current d-async host does not support yielding.");
    }

    protected virtual Task DelayAsync(DAsyncId id, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("The current d-async host does not support delaying.");
    }

    ITypeResolver IDAsyncHost.TypeResolver => TypeResolver;

    IDAsyncMarshaler IDAsyncHost.CreateMarshaler() => CreateMarshaler();

    IDAsyncStateManager IDAsyncHost.CreateStateManager(IDAsyncMarshaler marshaler) => CreateStateManager(marshaler);

    Task IDAsyncHost.SucceedAsync(CancellationToken cancellationToken) => SucceedAsync(cancellationToken);

    Task IDAsyncHost.SucceedAsync<TResult>(TResult result, CancellationToken cancellationToken) => SucceedAsync(result, cancellationToken);

    Task IDAsyncHost.FailAsync(Exception exception, CancellationToken cancellationToken) => FailAsync(exception, cancellationToken);

    Task IDAsyncHost.YieldAsync(DAsyncId id, CancellationToken cancellationToken) => YieldAsync(id, cancellationToken);

    Task IDAsyncHost.DelayAsync(DAsyncId id, TimeSpan delay, CancellationToken cancellationToken) => DelayAsync(id, delay, cancellationToken);
}
