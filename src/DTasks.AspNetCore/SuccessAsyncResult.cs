using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore;

internal class SuccessAsyncResult : IResult, IAsyncHttpResult
{
    public Task ExecuteAsync(HttpContext httpContext) => Results.Ok().ExecuteAsync(httpContext);

    public Task ExecuteAsync(IAsyncHttpResultHandler handler, CancellationToken cancellationToken = default)
    {
        return handler.SucceedAsync(cancellationToken);
    }
}

internal class SuccessAsyncResult<T>(T value) : IResult, IAsyncHttpResult
{
    public Task ExecuteAsync(HttpContext httpContext) => Results.Ok(value).ExecuteAsync(httpContext);

    public Task ExecuteAsync(IAsyncHttpResultHandler handler, CancellationToken cancellationToken = default)
    {
        return handler.SucceedAsync(value, cancellationToken);
    }
}
