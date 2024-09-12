using DTasks.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Extensions.Microsoft.DependencyInjection.Hosting;

internal class DTaskScope : IDTaskScope, IServiceMapper
{
    private readonly IServiceProvider _services;
    private readonly ServiceResolver _resolver;
    private readonly Dictionary<object, ServiceToken> _tokens;

    protected DTaskScope(IServiceProvider services, ServiceResolver resolver)
    {
        _services = services;
        _resolver = resolver;
        _tokens = [];
    }

    public void MapService(object service, ServiceToken token)
    {
        _tokens.Add(service, token);
    }

    public bool TryGetReference(object token, [NotNullWhen(true)] out object? reference)
    {
        if (token is not ServiceToken serviceToken)
        {
            reference = null;
            return false;
        }

        if (!ServiceTypeId.TryParse(serviceToken.TypeId, out ServiceTypeId typeId))
            return False(out reference);

        if (!_resolver.Invoke(typeId, out Type? serviceType))
            return False(out reference);

        reference = serviceToken is IKeyedServiceToken { Key: var serviceKey }
            ? _services.GetRequiredKeyedService(serviceType, serviceKey)
            : _services.GetRequiredService(serviceType);

        return true;

        static bool False(out object? reference)
        {
            reference = null;
            return false;
        }
    }

    public virtual bool TryGetReferenceToken(object reference, [NotNullWhen(true)] out object? token)
    {
        if (!_tokens.TryGetValue(reference, out ServiceToken? serviceToken))
        {
            token = null;
            return false;
        }

        token = serviceToken;
        return true;
    }
}
