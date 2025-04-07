using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore;

internal sealed class HttpRequestDAsyncHost(HttpContext httpContext) : Infrastructure.AspNetCoreDAsyncHost
{
    protected override IServiceProvider Services => httpContext.RequestServices;

    protected override Task OnSuspendAsync(CancellationToken cancellationToken)
    {
        return base.OnSuspendAsync(cancellationToken);
    }

    protected override Task SucceedAsync(CancellationToken cancellationToken)
    {
        // TODO: If configured, we can optionally also update the status for the status monitor
        return Results.Ok().ExecuteAsync(httpContext);
    }

    protected override Task SucceedAsync<TResult>(TResult result, CancellationToken cancellationToken)
    {
        var httpResult = result as IResult ?? Results.Ok(result);
        return httpResult.ExecuteAsync(httpContext);
    }
}