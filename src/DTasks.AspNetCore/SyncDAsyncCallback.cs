using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore;

public class SyncDAsyncCallback : IDAsyncCallback
{
    public IResult? Result { get; private set; }

    public Task SucceedAsync(CancellationToken cancellationToken = default)
    {
        Result = Results.Ok();
        return Task.CompletedTask;
    }

    public Task SucceedAsync<TResult>(TResult result, CancellationToken cancellationToken = default)
    {
        Result = result as IResult ?? Results.Ok(result);
        return Task.CompletedTask;
    }
}
