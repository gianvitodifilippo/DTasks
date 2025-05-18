using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using DTasks.AspNetCore.Http;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.AspNetCore.Execution;

using ResumptionEndpointFactory = Func<TypeId, IResumptionEndpoint>;

internal sealed class WebSuspensionRegisterBuilder
{
    private readonly HashSet<IResumptionEndpoint> _resumptionEndpoints = [];
    private readonly Dictionary<Type, ResumptionEndpointFactory> _defaultResumptionEndpointFactories = [];

    public void AddResumptionEndpoint(IResumptionEndpoint endpoint)
    {
        _resumptionEndpoints.Add(endpoint);
    }

    public void AddDefaultResumptionEndpoint<TResult>()
    {
        Debug.Assert(typeof(TResult) != typeof(void));
        
        _defaultResumptionEndpointFactories[typeof(TResult)] = static typeId => new ResumptionEndpoint<TResult>(
            DTasksHttpConstants.DTasksDefaultResumptionEndpointTemplate.Replace(
                $"{{{DTasksHttpConstants.TypeIdParameterName}}}",
                typeId.ToString()));
    }

    public IWebSuspensionRegister Build(IDAsyncTypeResolver typeResolver)
    {
        Dictionary<Type, IResumptionEndpoint> defaultResumptionEndpoints = [];
        foreach ((Type type, ResumptionEndpointFactory factory) in _defaultResumptionEndpointFactories)
        {
            TypeId typeId = typeResolver.GetTypeId(type);
            defaultResumptionEndpoints.Add(type, factory(typeId));
        }

        return new WebSuspensionRegister(_resumptionEndpoints.ToFrozenSet(), defaultResumptionEndpoints.ToFrozenDictionary());
    }
}