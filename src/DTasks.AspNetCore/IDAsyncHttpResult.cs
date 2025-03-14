namespace DTasks.AspNetCore;

internal interface IDAsyncHttpResult
{
    Task ExecuteAsync(IDAsyncContext context, CancellationToken cancellationToken = default);
}
