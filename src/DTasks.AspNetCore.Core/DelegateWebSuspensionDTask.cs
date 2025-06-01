using DTasks.AspNetCore.Execution;
using DTasks.Execution;
using DTasks.Extensions.DependencyInjection.Infrastructure.Features;
using DTasks.Infrastructure;
using DTasks.Infrastructure.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.AspNetCore;

internal sealed class DelegateWebSuspensionDTask<TResult>(
    IResumptionEndpoint? resumptionEndpoint,
    WebSuspensionCallback callback) : WebSuspensionDTaskBase<TResult>(resumptionEndpoint)
{
    protected override Task InvokeAsync(IWebSuspensionContext context, CancellationToken cancellationToken = default)
    {
        return callback(context, cancellationToken);
    }
}

internal sealed class DelegateWebSuspensionDTask<TState, TResult>(
    IResumptionEndpoint? resumptionEndpoint,
    TState state,
    WebSuspensionCallback<TState> callback) : WebSuspensionDTaskBase<TResult>(resumptionEndpoint)
{
    protected override Task InvokeAsync(IWebSuspensionContext context, CancellationToken cancellationToken = default)
    {
        return callback(context, state, cancellationToken);
    }
}
