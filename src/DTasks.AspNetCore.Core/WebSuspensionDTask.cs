using DTasks.AspNetCore.Execution;

namespace DTasks.AspNetCore;

internal sealed class WebSuspensionDTask<TCallback, TResult>(
    IResumptionEndpoint? resumptionEndpoint,
    TCallback callback) : WebSuspensionDTaskBase<TResult>(resumptionEndpoint)
    where TCallback : IWebSuspensionCallback
{
    private TCallback _callback = callback;

    protected override Task InvokeAsync(IWebSuspensionContext context, CancellationToken cancellationToken = default)
    {
        return _callback.InvokeAsync(context, cancellationToken);
    }
}
