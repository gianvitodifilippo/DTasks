namespace DTasks.AspNetCore.Infrastructure.Http;

public interface IDAsyncCallbackMemento
{
    IDAsyncCallback Restore(IServiceProvider services);
}