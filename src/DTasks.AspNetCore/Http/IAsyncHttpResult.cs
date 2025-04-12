using DTasks.Infrastructure;

namespace DTasks.AspNetCore.Http;

internal interface IAsyncHttpResult
{
    Task ExecuteAsync(IAsyncHttpResultHandler handler, IDAsyncFlowCompletionContext context, CancellationToken cancellationToken = default);
}
