namespace DTasks.Host;

public interface ICompletionHandler
{
    Task OnCompletedAsync(CancellationToken cancellationToken);

    Task OnCompletedAsync<TResult>(TResult result, CancellationToken cancellationToken);
}
