using DTasks;
using DTasks.AspNetCore.Execution;
using DTasks.AspNetCore.Http;
using DTasks.AspNetCore.Infrastructure.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing;

public static class DTasksAspNetCoreEndpointRouteBuilderExtensions
{
    public static void MapDTasks(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet(DTasksHttpConstants.DTasksStatusEndpointTemplate, GetStatusAsync)
            .WithName(DTasksHttpConstants.DTasksGetStatusEndpointName);
        
        var suspensionRegister = endpoints.ServiceProvider.GetRequiredService<IWebSuspensionRegister>();
        suspensionRegister.MapResumptionEndpoints(endpoints);
    }
    
    private static Task GetStatusAsync(HttpContext httpContext)
    {
        string operationId = (string)httpContext.Request.RouteValues[DTasksHttpConstants.OperationIdParameterName]!;
        if (!DAsyncId.TryParse(operationId, out DAsyncId flowId))
            return Results.NotFound().ExecuteAsync(httpContext);

        var monitor = httpContext.RequestServices.GetRequiredService<EndpointStatusMonitor>();
        return monitor.ExecuteGetStatusAsync(httpContext, flowId);
    }
}