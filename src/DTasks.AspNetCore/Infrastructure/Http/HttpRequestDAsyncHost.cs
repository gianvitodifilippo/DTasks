using System.Diagnostics;
using DTasks.AspNetCore.Http;
using DTasks.Infrastructure;
using DTasks.Infrastructure.Marshaling;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace DTasks.AspNetCore.Infrastructure.Http;

internal sealed class HttpRequestDAsyncHost(HttpContext httpContext, string monitorActionName) : AspNetCoreDAsyncHost, IAsyncHttpResultHandler
{
    protected override IServiceProvider Services => httpContext.RequestServices;

    protected override Task OnStartCoreAsync(IDAsyncFlowStartContext context, CancellationToken cancellationToken)
    {
        IHeaderDictionary headers = httpContext.Request.Headers;
        if (!headers.TryGetValue(AsyncHeaders.CallbackType, out StringValues callbackTypeHeaderValues))
            return Task.CompletedTask;
        
        int callbackTypeHeaderCount = callbackTypeHeaderValues.Count;
        if (callbackTypeHeaderCount == 0)
            return Task.CompletedTask;
        
        var continuationFactory = httpContext.RequestServices.GetRequiredService<IDAsyncContinuationFactory>();
        
        if (callbackTypeHeaderCount == 1)
        {
            var callbackType = (string?)callbackTypeHeaderValues;
            if (callbackType is null)
                return OnNullCallbackHeaderAsync(cancellationToken);
            
            if (!continuationFactory.TryCreateSurrogate(callbackType, headers, out TypedInstance<object> continuationSurrogate))
                return OnUnsupportedCallbackTypeAsync(callbackType, cancellationToken);
            
            SetContinuation(continuationSurrogate);
            return Task.CompletedTask;
        }

        var callbackTypes = (string?[]?)callbackTypeHeaderValues;
        Debug.Assert(callbackTypes is not null, "When a header contains multiple values, we should have an array.");

        TypedInstance<object>[] continuationSurrogateArray = new TypedInstance<object>[callbackTypes.Length];
        List<string>? unsupportedCallbackTypes = null;
        for (var i = 0; i < callbackTypes.Length; i++)
        {
            string? callbackType = callbackTypes[i];
            if (callbackType is null)
                return OnNullCallbackHeaderAsync(cancellationToken);

            if (!continuationFactory.TryCreateSurrogate(callbackType, headers, out continuationSurrogateArray[i]))
            {
                unsupportedCallbackTypes ??= new List<string>(1);
                unsupportedCallbackTypes.Add(callbackType);
            }
        }
        
        if (unsupportedCallbackTypes is not null)
            return OnUnsupportedCallbackTypesAsync(unsupportedCallbackTypes, cancellationToken);
        
        SetContinuation(continuationSurrogateArray);
        return Task.CompletedTask;
    }

    protected override Task SuspendOnStartAsync(CancellationToken cancellationToken)
    {
        var value = new { operationId = FlowId.ToString() };
        return Results.AcceptedAtRoute(monitorActionName, value, value).ExecuteAsync(httpContext);
    }

    protected override Task SucceedOnStartAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken)
    {
        return Results.Ok().ExecuteAsync(httpContext);
    }

    protected override Task SucceedOnStartAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, CancellationToken cancellationToken)
    {
        var httpResult = result as IResult ?? Results.Ok(result);
        return httpResult.ExecuteAsync(httpContext);
    }

    protected override Task FailOnStartAsync(IDAsyncFlowCompletionContext context, Exception exception, CancellationToken cancellationToken)
    {
        var httpResult = Results.StatusCode(500); // TODO
        return httpResult.ExecuteAsync(httpContext);
    }

    protected override Task CancelOnStartAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken)
    {
        if (httpContext.RequestAborted.IsCancellationRequested)
            return Task.CompletedTask;

        throw new NotImplementedException();
    }

    protected override Task SucceedOnResumeAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, CancellationToken cancellationToken)
    {
        if (result is IResult)
        {
            if (result is not IAsyncHttpResult httpResult)
                throw new InvalidOperationException("Unsupported result type returned from a d-async endpoint. Use DAsyncResults to return from a d-async method.");

            return httpResult.ExecuteAsync(this, context, cancellationToken);
        }
        
        return base.SucceedOnResumeAsync(context, result, cancellationToken);
    }

    Task IAsyncHttpResultHandler.SucceedAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken)
    {
        return SucceedOnResumeAsync(context, cancellationToken);
    }

    Task IAsyncHttpResultHandler.SucceedAsync<TResult>(IDAsyncFlowCompletionContext context,TResult result, CancellationToken cancellationToken)
    {
        return SucceedOnResumeAsync(context, result, cancellationToken);
    }

    private Task OnNullCallbackHeaderAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement with proper response
        return Results.BadRequest().ExecuteAsync(httpContext);
    }

    private Task OnUnsupportedCallbackTypeAsync(string callbackType, CancellationToken cancellationToken)
    {
        // TODO: Implement with proper response
        return Results.BadRequest().ExecuteAsync(httpContext);
    }

    private Task OnUnsupportedCallbackTypesAsync(List<string> callbackTypes, CancellationToken cancellationToken)
    {
        // TODO: Implement with proper response
        return Results.BadRequest().ExecuteAsync(httpContext);
    }
}
