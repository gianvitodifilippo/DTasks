using System.ComponentModel;
using DTasks.Utils;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class DAsyncRunner : IDisposable
{
    protected abstract IDAsyncHostInfrastructure InfrastructureCore { get; }
    
    public IDAsyncHostInfrastructure Infrastructure => InfrastructureCore;
    
    protected abstract ValueTask StartCoreAsync(IDAsyncRunnable runnable, CancellationToken cancellationToken);

    protected abstract ValueTask ResumeCoreAsync(DAsyncId id, CancellationToken cancellationToken);

    protected abstract ValueTask ResumeCoreAsync<TResult>(DAsyncId id, TResult result, CancellationToken cancellationToken);

    protected abstract ValueTask ResumeCoreAsync(DAsyncId id, Exception exception, CancellationToken cancellationToken);

    public ValueTask StartAsync(IDAsyncRunnable runnable, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(runnable);

        return StartCoreAsync(runnable, cancellationToken);
    }

    public ValueTask ResumeAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        return ResumeCoreAsync(id, cancellationToken);
    }

    public ValueTask ResumeAsync<TResult>(DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        return ResumeCoreAsync(id, result, cancellationToken);
    }

    public ValueTask ResumeAsync(DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(exception);

        return ResumeCoreAsync(id, exception, cancellationToken);
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}