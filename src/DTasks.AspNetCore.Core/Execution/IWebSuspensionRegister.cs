using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Routing;

namespace DTasks.AspNetCore.Execution;

internal interface IWebSuspensionRegister
{
    bool IsRegistered(IResumptionEndpoint resumptionEndpoint);
    
    bool TryGetDefaultResumptionEndpoint(Type resultType, [NotNullWhen(true)] out IResumptionEndpoint? resumptionEndpoint);

    void MapResumptionEndpoints(IEndpointRouteBuilder endpoints);
}
