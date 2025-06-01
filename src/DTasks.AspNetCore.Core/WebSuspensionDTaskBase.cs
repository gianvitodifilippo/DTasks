using DTasks.AspNetCore.Execution;
using DTasks.AspNetCore.Infrastructure.Features;
using DTasks.Configuration;
using DTasks.Execution;
using DTasks.Extensions.DependencyInjection.Infrastructure.Features;
using DTasks.Infrastructure;
using DTasks.Infrastructure.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DTasks.AspNetCore;

internal abstract class WebSuspensionDTaskBase<TResult>(IResumptionEndpoint? resumptionEndpoint) : DTask<TResult>, ISuspensionCallback, IWebSuspensionContext
{
    private IResumptionEndpoint? _resumptionEndpoint = resumptionEndpoint;
    private bool _hasRun;
    private PathString _callbackBasePath;
    private DAsyncId _operationId;
    private string? _callbackUrl;
    private string? _callbackPath;

    public sealed override DTaskStatus Status => DTaskStatus.Suspended;

    DAsyncId IWebSuspensionContext.OperationId => _operationId;

    string IWebSuspensionContext.CallbackUrl => _callbackUrl ??= _resumptionEndpoint!.MakeCallbackUrl(_callbackBasePath, _operationId);

    string IWebSuspensionContext.CallbackPath => _callbackPath ??= _resumptionEndpoint!.MakeCallbackPath(_operationId);

    protected abstract Task InvokeAsync(IWebSuspensionContext context, CancellationToken cancellationToken = default);
    
    protected sealed override void Run(IDAsyncRunner runner)
    {
        if (_hasRun)
            throw new InvalidOperationException("Detected invalid usage of WebSuspend.");
        
        _hasRun = true;
        
        ISuspensionFeature suspensionFeature = runner.Features.GetRequiredFeature<ISuspensionFeature>();
        IServiceProvider services = runner.Features.GetRequiredFeature<IServiceProviderFeature>().Services;
        
        var register = services.GetRequiredService<IWebSuspensionRegister>();
        if (_resumptionEndpoint is not null)
        {
            if (!register.IsRegistered(_resumptionEndpoint))
                throw new InvalidOperationException("The provided resumption endpoint is not registered.");
        }
        else
        {
            if (!register.TryGetDefaultResumptionEndpoint(typeof(TResult), out _resumptionEndpoint))
                throw new InvalidOperationException($"No default resumption endpoint registered for type '{typeof(TResult).Name}'.");
        }
        
        var options = services.GetRequiredService<IOptions<DTasksAspNetCoreOptions>>().Value;
        
        _callbackBasePath = options.CallbackBasePath;
        if (!_callbackBasePath.HasValue)
        {
            HttpContext? httpContext = runner.Features.GetFeature<IHttpContextFeature>()?.HttpContext;
            if (httpContext is null)
                throw new InvalidOperationException("CallbackBasePath was not specified."); // TODO: Improve error message
            
            _callbackBasePath = httpContext.Request.PathBase;
        }
        
        suspensionFeature.Suspend(this);
    }
    
    Task ISuspensionCallback.InvokeAsync(DAsyncId id, CancellationToken cancellationToken)
    {
        _operationId = id;
        return InvokeAsync(this, cancellationToken);
    }
}