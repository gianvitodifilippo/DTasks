namespace DTasks.AspNetCore.Infrastructure.Http;

public interface IDAsyncContinuation
{
    Task OnSucceedAsync(DAsyncId flowId, CancellationToken cancellationToken = default);

    Task OnSucceedAsync<TResult>(DAsyncId flowId, TResult result, CancellationToken cancellationToken = default);

    Task OnFailAsync(DAsyncId flowId, Exception exception, CancellationToken cancellationToken = default);
    
    Task OnCancelAsync(DAsyncId flowId, CancellationToken cancellationToken = default);
}
