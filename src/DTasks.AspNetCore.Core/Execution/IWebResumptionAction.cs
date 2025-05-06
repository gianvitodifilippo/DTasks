using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore.Execution;

internal interface IWebResumptionAction
{
    ValueTask ResumeAsync(HttpContext httpContext, DAsyncId id);
}