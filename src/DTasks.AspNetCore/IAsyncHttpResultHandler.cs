namespace DTasks.AspNetCore;

internal interface IAsyncHttpResultHandler
{
    Task SucceedAsync(CancellationToken cancellationToken = default);

    Task SucceedAsync<TResult>(TResult result, CancellationToken cancellationToken = default);
}
