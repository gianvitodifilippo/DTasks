namespace DTasks.AspNetCore.Infrastructure.Http;

internal sealed class AggregateDAsyncCallback(IDAsyncCallback[] callbacks) : IDAsyncCallback
{
    // TODO: Support parallel execution when configured
    
    public async Task SucceedAsync(DAsyncOperationId operationId, CancellationToken cancellationToken = default)
    {
        foreach (IDAsyncCallback callback in callbacks)
        {
            await callback.SucceedAsync(operationId, cancellationToken);
        }
    }

    public async Task SucceedAsync<TResult>(DAsyncOperationId operationId, TResult result,
        CancellationToken cancellationToken = default)
    {
        foreach (IDAsyncCallback callback in callbacks)
        {
            await callback.SucceedAsync(operationId, result, cancellationToken);
        }
    }

    public async Task FailAsync(DAsyncOperationId operationId, Exception exception, CancellationToken cancellationToken = default)
    {
        foreach (IDAsyncCallback callback in callbacks)
        {
            await callback.FailAsync(operationId, exception, cancellationToken);
        }
    }

    private sealed class Memento : IDAsyncCallbackMemento
    {
        public IDAsyncCallback Restore(IServiceProvider services)
        {
            throw new NotImplementedException();
        }
    }
}