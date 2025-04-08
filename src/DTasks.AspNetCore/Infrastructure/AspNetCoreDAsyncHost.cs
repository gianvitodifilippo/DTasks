using DTasks.AspNetCore.Http;
using DTasks.AspNetCore.Infrastructure.Http;
using DTasks.Infrastructure;
using DTasks.Infrastructure.Execution;
using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore.Infrastructure;

public abstract partial class AspNetCoreDAsyncHost : IAsyncHttpResultHandler
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
        return DAsyncFlow.ResumeAsync(ExecutionMode.Snapshot, this, id, result, cancellationToken);
    }

    public ValueTask ResumeAsync(DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        return DAsyncFlow.ResumeAsync(ExecutionMode.Snapshot, this, id, exception, cancellationToken);
    }

    Task IAsyncHttpResultHandler.SucceedAsync(CancellationToken cancellationToken) => SucceedAsync(cancellationToken);

    Task IAsyncHttpResultHandler.SucceedAsync<TResult>(TResult result, CancellationToken cancellationToken) =>
        SucceedAsync(result, cancellationToken);

    public static AspNetCoreDAsyncHost CreateHttpHost(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        return new HttpRequestDAsyncHost(httpContext);
    }
}
