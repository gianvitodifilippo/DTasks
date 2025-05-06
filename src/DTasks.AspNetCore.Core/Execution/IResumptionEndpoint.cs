using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore.Execution;

internal interface IResumptionEndpoint
{
    string Pattern { get; }

    string MakeCallbackUrl(string basePath, DAsyncId operationId);

    string MakeCallbackPath(DAsyncId operationId);
    
    Task ResumeAsync(HttpContext httpContext);
}