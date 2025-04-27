namespace DTasks.AspNetCore.Infrastructure.Http;

public interface IDAsyncContinuationSurrogate
{
    IDAsyncContinuation Restore(IServiceProvider services);
}