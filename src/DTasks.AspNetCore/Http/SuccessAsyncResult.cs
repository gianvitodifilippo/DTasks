using DTasks.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore.Http;

internal class SuccessAsyncResult : IResult, IAsyncHttpResult
{
    public Task ExecuteAsync(HttpContext httpContext) => Results.Ok().ExecuteAsync(httpContext);

    public Task ExecuteAsync(IAsyncHttpResultHandler handler, IDAsyncFlowCompletionContext context,CancellationToken cancellationToken = default)
    {
        return handler.SucceedAsync(context, cancellationToken);
    }
}

internal class SuccessAsyncResult<T>(T value) : IResult, IAsyncHttpResult
{
    public Task ExecuteAsync(HttpContext httpContext) => Results.Ok(value).ExecuteAsync(httpContext);

    public Task ExecuteAsync(IAsyncHttpResultHandler handler, IDAsyncFlowCompletionContext context,CancellationToken cancellationToken = default)
    {
        return handler.SucceedAsync(context, value, cancellationToken);
    }
}
