namespace DTasks.AspNetCore;

public interface IDAsyncContext
{
    Task SucceedAsync(CancellationToken cancellationToken = default);

    Task SucceedAsync<TResult>(TResult result, CancellationToken cancellationToken = default);
}
