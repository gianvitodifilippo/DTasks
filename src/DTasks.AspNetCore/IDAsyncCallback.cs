namespace DTasks.AspNetCore;

public interface IDAsyncCallback
{
    Task SucceedAsync(CancellationToken cancellationToken = default);

    Task SucceedAsync<TResult>(TResult result, CancellationToken cancellationToken = default);
}
