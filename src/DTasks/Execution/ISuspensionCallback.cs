namespace DTasks.Execution;

public interface ISuspensionCallback
{
    Task InvokeAsync(DAsyncId id, CancellationToken cancellationToken = default);
}

