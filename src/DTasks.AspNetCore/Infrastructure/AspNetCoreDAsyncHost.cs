using DTasks.AspNetCore.Infrastructure.Http;
using DTasks.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore.Infrastructure;

public abstract partial class AspNetCoreDAsyncHost
{
    protected abstract IServiceProvider Services { get; }

    public ValueTask StartAsync(IDAsyncRunnable runnable, CancellationToken cancellationToken = default)
    {
        return DAsyncFlow.StartFlowAsync(this, runnable, cancellationToken);
    }
    
    public ValueTask ResumeAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        return DAsyncFlow.ResumeFlowAsync(this, id, cancellationToken);
    }

    public ValueTask ResumeAsync<TResult>(DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        return DAsyncFlow.ResumeFlowAsync(this, id, result, cancellationToken);
    }

    public ValueTask ResumeAsync(DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        return DAsyncFlow.ResumeFlowAsync(this, id, exception, cancellationToken);
    }

    public static AspNetCoreDAsyncHost CreateHttpHost(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        return new HttpRequestDAsyncHost(httpContext);
    }
}
