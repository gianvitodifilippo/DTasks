namespace DTasks.AspNetCore;

internal interface IDAsyncHttpResult
{
    Task ExecuteAsync(IAsyncResultHandler handler, CancellationToken cancellationToken = default);
}
