namespace DTasks.AspNetCore.Execution;

public interface IWebSuspensionCallback
{
    Task InvokeAsync(IWebSuspensionContext context, CancellationToken cancellationToken = default);
}
