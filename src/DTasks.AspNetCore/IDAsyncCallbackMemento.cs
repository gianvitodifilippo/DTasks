namespace DTasks.AspNetCore;

public interface IDAsyncCallbackMemento
{
    IDAsyncCallback Restore(IServiceProvider services);
}