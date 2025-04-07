using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore;

public static class DAsyncHosting
{
    public static IDAsyncStarter CreateHttpStarter(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        
        return new HttpRequestDAsyncHost(httpContext);
    }
}