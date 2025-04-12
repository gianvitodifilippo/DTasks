namespace DTasks.AspNetCore.Infrastructure.Http;

public interface IDAsyncContinuationMemento
{
    IDAsyncContinuation Restore(IServiceProvider services);
}