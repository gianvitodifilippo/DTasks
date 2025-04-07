namespace DTasks.AspNetCore;

internal interface IDAsyncStatusManager
{
    Task CompleteAsync(DAsyncOperationId id, CancellationToken cancellationToken);
    
    Task CompleteAsync<TResult>(DAsyncOperationId id, TResult result, CancellationToken cancellationToken);
    
    Task CompleteAsync(DAsyncOperationId id, Exception exception, CancellationToken cancellationToken);
}