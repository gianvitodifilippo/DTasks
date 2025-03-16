using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore;

internal class SuccessDAsyncResult : IResult, IDAsyncHttpResult
{
    public Task ExecuteAsync(HttpContext httpContext) => Results.Ok().ExecuteAsync(httpContext);

    public Task ExecuteAsync(IDAsyncContext context, CancellationToken cancellationToken = default)
    {
        return context.SucceedAsync(cancellationToken);
    }
}

internal class SuccessDAsyncResult<T>(T value) : IResult, IDAsyncHttpResult
{
    public Task ExecuteAsync(HttpContext httpContext) => Results.Ok(value).ExecuteAsync(httpContext);

    public Task ExecuteAsync(IDAsyncContext context, CancellationToken cancellationToken = default)
    {
        return context.SucceedAsync(value, cancellationToken);
    }
}
