using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace DTasks.AspNetCore.Execution;

internal sealed class WebSuspensionRegister(
    FrozenSet<IResumptionEndpoint> resumptionEndpoints,
    FrozenDictionary<Type, IResumptionEndpoint> defaultResumptionEndpoints) : IWebSuspensionRegister
{
    public bool IsRegistered(IResumptionEndpoint resumptionEndpoint)
    {
        return resumptionEndpoints.Contains(resumptionEndpoint);
    }

    public bool TryGetDefaultResumptionEndpoint(Type resultType, [NotNullWhen(true)] out IResumptionEndpoint? resumptionEndpoint)
    {
        return defaultResumptionEndpoints.TryGetValue(resultType, out resumptionEndpoint);
    }

    public void MapResumptionEndpoints(IEndpointRouteBuilder endpoints)
    {
        foreach (IResumptionEndpoint resumptionEndpoint in resumptionEndpoints)
        {
            endpoints.MapPost(resumptionEndpoint.Pattern, resumptionEndpoint.ResumeAsync);
        }
    }
}