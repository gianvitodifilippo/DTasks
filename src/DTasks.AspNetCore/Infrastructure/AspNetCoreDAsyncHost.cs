using DTasks.Infrastructure;
using DTasks.Infrastructure.Execution;

namespace DTasks.AspNetCore.Infrastructure;

internal abstract partial class AspNetCoreDAsyncHost : IDAsyncStarter, IDAsyncResumer, IAsyncResultHandler
{
    protected abstract IServiceProvider Services { get; }

    protected abstract Task SucceedAsync(CancellationToken cancellationToken);
    
    protected abstract Task SucceedAsync<TResult>(TResult result, CancellationToken cancellationToken);

    // TODO: Support ExecutionMode.Replay as well
    public ValueTask StartAsync(IDAsyncRunnable runnable, CancellationToken cancellationToken = default)
    {
        return DAsyncFlow.StartAsync(ExecutionMode.Snapshot, this, runnable, cancellationToken);
    }
    
    public ValueTask ResumeAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        return DAsyncFlow.ResumeAsync(ExecutionMode.Snapshot, this, id, cancellationToken);
    }

    public ValueTask ResumeAsync<TResult>(DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        return DAsyncFlow.ResumeAsync<TResult>(ExecutionMode.Snapshot, this, id, result, cancellationToken);
    }

    public ValueTask ResumeAsync(DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        return DAsyncFlow.ResumeAsync(ExecutionMode.Snapshot, this, id, exception, cancellationToken);
    }

    Task IAsyncResultHandler.SucceedAsync(CancellationToken cancellationToken) => SucceedAsync(cancellationToken);

    Task IAsyncResultHandler.SucceedAsync<TResult>(TResult result, CancellationToken cancellationToken) =>
        SucceedAsync(result, cancellationToken);
}
