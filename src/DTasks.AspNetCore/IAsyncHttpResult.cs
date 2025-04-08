namespace DTasks.AspNetCore;

internal interface IAsyncHttpResult
{
    Task ExecuteAsync(IAsyncHttpResultHandler handler, CancellationToken cancellationToken = default);
}
