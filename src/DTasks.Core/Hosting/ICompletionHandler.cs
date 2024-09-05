namespace DTasks.Hosting;

public interface ICompletionHandler
{
    Task OnCompletedAsync(CancellationToken cancellationToken);

    Task OnCompletedAsync<TResult>(TResult result, CancellationToken cancellationToken);
}
