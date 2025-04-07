using DTasks.Infrastructure;

namespace DTasks.AspNetCore;

public interface IDAsyncResumer
{
    ValueTask ResumeAsync(DAsyncId id, CancellationToken cancellationToken = default);
    
    ValueTask ResumeAsync<TResult>(DAsyncId id, TResult result, CancellationToken cancellationToken = default);
    
    ValueTask ResumeAsync(DAsyncId id, Exception exception, CancellationToken cancellationToken = default);
}