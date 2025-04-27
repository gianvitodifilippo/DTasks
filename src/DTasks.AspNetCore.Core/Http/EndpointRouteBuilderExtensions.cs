using System.Diagnostics;
using DTasks.AspNetCore.Infrastructure.Http;
using DTasks.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.AspNetCore.Http;

public static class EndpointRouteBuilderExtensions
{
    private static readonly int s_endpointPrefixLength = DTasksHttpConstants.DTasksEndpointPrefix.Length + 1;
    
    public static IEndpointRouteBuilder MapDTasks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.Map(DTasksHttpConstants.DTasksStatusEndpoint, GetStatusAsync);
        
        return endpoints;
    }

    private static Task GetStatusAsync(HttpContext httpContext)
    {
        Debug.Assert(httpContext.Request.Path.HasValue);
            
        string path = httpContext.Request.Path.Value;
        ReadOnlySpan<char> flowIdSpan = path.AsSpan()[s_endpointPrefixLength..];

        if (!DAsyncId.TryParse(flowIdSpan, out DAsyncId flowId))
            return Results.NotFound().ExecuteAsync(httpContext);

        return GetStatusCoreAsync(httpContext, flowId);
    }

    private static async Task GetStatusCoreAsync(HttpContext httpContext, DAsyncId flowId)
    {
        var endpointMonitor = httpContext.RequestServices.GetRequiredService<IAsyncEndpointMonitor>();
        Option<AsyncEndpointInfo> infoOption = await endpointMonitor.GetEndpointInfoAsync(flowId, httpContext.RequestAborted);
        
        IResult result = infoOption.Fold(Results.Ok, () => Results.NotFound());
        await result.ExecuteAsync(httpContext);
    }
}