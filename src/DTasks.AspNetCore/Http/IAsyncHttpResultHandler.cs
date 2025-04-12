using DTasks.Infrastructure;

namespace DTasks.AspNetCore.Http;

internal interface IAsyncHttpResultHandler
{
    Task SucceedAsync(IDAsyncFlowCompletionContext context,CancellationToken cancellationToken = default);

    Task SucceedAsync<TResult>(IDAsyncFlowCompletionContext context,TResult result, CancellationToken cancellationToken = default);
}
