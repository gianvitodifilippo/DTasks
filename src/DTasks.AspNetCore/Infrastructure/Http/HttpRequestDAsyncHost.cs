using System.Diagnostics;
using DTasks.AspNetCore.Http;
using DTasks.Infrastructure;
using DTasks.Infrastructure.Marshaling;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace DTasks.AspNetCore.Infrastructure.Http;

internal sealed class HttpRequestDAsyncHost(HttpContext httpContext) : AspNetCoreDAsyncHost
{
    private DAsyncOperationId _operationId;
    private TypedInstance<object> _callbackMemento;
    private TypedInstance<object>[]? _callbackMementoArray;

    private bool IsStarting => _operationId != default;
    
    protected override IServiceProvider Services => httpContext.RequestServices;

    protected override Task OnStartAsync(IDAsyncResultBuilder resultBuilder, CancellationToken cancellationToken)
    {
        _operationId = DAsyncOperationId.New();
        IHeaderDictionary headers = httpContext.Request.Headers;
        if (!headers.TryGetValue(AsyncHeaders.CallbackType, out StringValues callbackTypeHeaderValues))
            return Task.CompletedTask;
        
        int callbackTypeHeaderCount = callbackTypeHeaderValues.Count;
        if (callbackTypeHeaderCount == 0)
            return Task.CompletedTask;
        
        var callbackFactory = httpContext.RequestServices.GetRequiredService<IDAsyncCallbackFactory>();
        
        if (callbackTypeHeaderCount == 1)
        {
            var callbackType = (string?)callbackTypeHeaderValues;
            if (callbackType is null)
                return OnNullCallbackHeaderAsync(cancellationToken);
            
            if (!callbackFactory.TryCreateMemento(callbackType, headers, out _callbackMemento))
                return OnUnsupportedCallbackTypeAsync(callbackType, cancellationToken);
            
            return Task.CompletedTask;
        }

        var callbackTypes = (string?[]?)callbackTypeHeaderValues;
        Debug.Assert(callbackTypes is not null, "When a header contains multiple values, we should have an array.");

        _callbackMementoArray = new TypedInstance<object>[callbackTypes.Length];
        List<string>? unsupportedCallbackTypes = null;
        for (var i = 0; i < callbackTypes.Length; i++)
        {
            string? callbackType = callbackTypes[i];
            if (callbackType is null)
                return OnNullCallbackHeaderAsync(cancellationToken);

            if (!callbackFactory.TryCreateMemento(callbackType, headers, out _callbackMementoArray[i]))
            {
                unsupportedCallbackTypes ??= new List<string>(1);
                unsupportedCallbackTypes.Add(callbackType);
            }
        }
        
        if (unsupportedCallbackTypes is not null)
            return OnUnsupportedCallbackTypesAsync(unsupportedCallbackTypes, cancellationToken);
        
        return Task.CompletedTask;
    }

    protected override Task OnSuspendAsync(CancellationToken cancellationToken)
    {
        if (IsStarting)
        {
            _isStarting = false;
            StateManager.Heap.SaveAsync()
            return;
        }
    }

    protected override Task SucceedAsync(CancellationToken cancellationToken)
    {
        if (_isStarting)
        {
            _isStarting = false;
            
            // TODO: If configured, we can optionally also update the status for the status monitor
            return Results.Ok().ExecuteAsync(httpContext);
        }

        throw new NotImplementedException();
    }

    protected override Task SucceedAsync<TResult>(TResult result, CancellationToken cancellationToken)
    {
        if (_isStarting)
        {
            _isStarting = false;
            
            // TODO: If configured, we can optionally also update the status for the status monitor
            var httpResult = result as IResult ?? Results.Ok(result);
            return httpResult.ExecuteAsync(httpContext);
        }

        throw new NotImplementedException();
    }

    private Task OnNullCallbackHeaderAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement
        return Results.BadRequest().ExecuteAsync(httpContext);
    }

    private Task OnUnsupportedCallbackTypeAsync(string callbackType, CancellationToken cancellationToken)
    {
        // TODO: Implement
        return Results.BadRequest().ExecuteAsync(httpContext);
    }

    private Task OnUnsupportedCallbackTypesAsync(List<string> callbackTypes, CancellationToken cancellationToken)
    {
        // TODO: Implement
        return Results.BadRequest().ExecuteAsync(httpContext);
    }

    private void Reset()
    {
        _operationId = default;
        _callbackMemento = default;
        _callbackMementoArray = null;
    }
}