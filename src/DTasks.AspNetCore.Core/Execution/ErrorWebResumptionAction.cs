using DTasks.AspNetCore.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore.Execution;

internal sealed class ErrorWebResumptionAction : IWebResumptionAction
{
    public ValueTask ResumeAsync(HttpContext httpContext, DAsyncId id)
    {
        AspNetCoreDAsyncHost host = AspNetCoreDAsyncHost.Create(httpContext.RequestServices);
        throw new NotImplementedException();
    }
}