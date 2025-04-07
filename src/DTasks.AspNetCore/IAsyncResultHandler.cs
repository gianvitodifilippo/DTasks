namespace DTasks.AspNetCore;

internal interface IAsyncResultHandler
{
    Task SucceedAsync(CancellationToken cancellationToken = default);

    Task SucceedAsync<TResult>(TResult result, CancellationToken cancellationToken = default);
}
