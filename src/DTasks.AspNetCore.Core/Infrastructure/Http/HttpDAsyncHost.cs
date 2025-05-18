using System.Diagnostics;
using DTasks.AspNetCore.Http;
using DTasks.AspNetCore.Infrastructure.Features;
using DTasks.Infrastructure;
using DTasks.Infrastructure.Marshaling;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace DTasks.AspNetCore.Infrastructure.Http;

internal sealed class HttpDAsyncHost(HttpContext httpContext) : AspNetCoreDAsyncHost, IHttpContextFeature
{
    protected override IServiceProvider Services => httpContext.RequestServices;

    HttpContext IHttpContextFeature.HttpContext => httpContext;

    protected override void OnInitialize(IDAsyncFlowInitializationContext context)
    {
        // TODO: We should probably surrogate the HttpContext, but not with the default mechanism
        base.OnInitialize(context);
        context.SetFeature<IHttpContextFeature>(this);
    }

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

    protected override Task SuspendOnStartAsync(DAsyncId flowId, CancellationToken cancellationToken)
    {
        var value = new { operationId = flowId.ToString() };
        return Results.AcceptedAtRoute(DTasksHttpConstants.DTasksGetStatusEndpointName, value, value).ExecuteAsync(httpContext);
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
