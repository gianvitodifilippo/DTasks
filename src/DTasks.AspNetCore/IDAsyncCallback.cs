namespace DTasks.AspNetCore;

public interface IDAsyncCallback
{
    Task SucceedAsync(DAsyncOperationId operationId, CancellationToken cancellationToken = default);

    Task SucceedAsync<TResult>(DAsyncOperationId operationId, TResult result, CancellationToken cancellationToken = default);

    Task FailAsync(DAsyncOperationId operationId, Exception exception, CancellationToken cancellationToken = default);
}
