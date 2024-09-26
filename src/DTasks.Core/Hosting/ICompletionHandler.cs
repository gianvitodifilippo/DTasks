namespace DTasks.Hosting;

public interface ICompletionHandler
{
    Task OnCompletedAsync(CancellationToken cancellationToken = default);

    Task OnCompletedAsync<TResult>(TResult result, CancellationToken cancellationToken = default);
}
