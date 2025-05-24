using DTasks;
using DTasks.AspNetCore.Execution;
using DTasks.AspNetCore.Http;
using DTasks.AspNetCore.Infrastructure.Http;
using DTasks.Configuration;
using DTasks.Extensions.DependencyInjection.Infrastructure;
using DTasks.Infrastructure;
using DTasks.Infrastructure.State;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing;

public static partial class DTasksAspNetCoreEndpointRouteBuilderExtensions
{
    public static void MapDTasks(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet(DTasksHttpConstants.DTasksStatusEndpointTemplate, GetStatusAsync)
            .WithName(DTasksHttpConstants.DTasksGetStatusEndpointName);
        
        var suspensionRegister = endpoints.ServiceProvider.GetRequiredService<IWebSuspensionRegister>();
        suspensionRegister.MapResumptionEndpoints(endpoints);
    }
    
    private static async Task GetStatusAsync(HttpContext httpContext)
    {
        string operationId = (string)httpContext.Request.RouteValues[DTasksHttpConstants.OperationIdParameterName]!;
        if (!DAsyncId.TryParse(operationId, out DAsyncId flowId))
        {
            await Results.NotFound().ExecuteAsync(httpContext);
            return;
        }

        var configuration = httpContext.RequestServices.GetRequiredService<DTasksConfiguration>();
        DAsyncRunner runner = configuration.CreateRunner(ServicedDAsyncHost.CreateDefault(httpContext.RequestServices));
        IDAsyncHeap heap = runner.Infrastructure.GetHeap();
        
        await httpContext.ExecuteGetStatusAsync(flowId, heap);
    }
}