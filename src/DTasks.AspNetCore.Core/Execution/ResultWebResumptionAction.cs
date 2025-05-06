using DTasks.AspNetCore.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore.Execution;

internal sealed class ResultWebResumptionAction<TResult> : IWebResumptionAction
{
    public async ValueTask ResumeAsync(HttpContext httpContext, DAsyncId id)
    {
        TResult? result = await httpContext.Request.ReadFromJsonAsync<TResult>(httpContext.RequestAborted);
        
        AspNetCoreDAsyncHost host = AspNetCoreDAsyncHost.Create(httpContext.RequestServices);
        await host.ResumeAsync(id, result, httpContext.RequestAborted);
    }
}