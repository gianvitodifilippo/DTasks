using DTasks.AspNetCore.Infrastructure;
using DTasks.Configuration;
using DTasks.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http;

public static class DTasksHttpContextExtensions
{
    public static Task RunAsync(this HttpContext httpContext, IDAsyncRunnable runnable)
    {
        var configuration = httpContext.RequestServices.GetRequiredService<DTasksConfiguration>();
        var host = AspNetCoreDAsyncHost.CreateAsyncEndpointHost(httpContext);

        return configuration.StartAsync(host, runnable, httpContext.RequestAborted);
    }
}
