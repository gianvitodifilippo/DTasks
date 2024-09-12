using DTasks.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Extensions.Microsoft.DependencyInjection.Hosting;

internal sealed class DTaskScope(IServiceProvider services, ServiceResolver resolver, DTaskScope? parent) : IDTaskScope
{
    private readonly Dictionary<object, ServiceToken> _tokens = [];

    public void MarkService(object service, ServiceToken token)
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

        if (!resolver.Invoke(typeId, out Type? serviceType))
            return False(out reference);

        reference = serviceToken is IKeyedServiceToken { Key: var serviceKey }
            ? services.GetRequiredKeyedService(serviceType, serviceKey)
            : services.GetRequiredService(serviceType);

        return true;

        static bool False(out object? reference)
        {
            reference = null;
            return false;
        }
    }

    public bool TryGetReferenceToken(object reference, [NotNullWhen(true)] out object? token)
    {
        if (!_tokens.TryGetValue(reference, out ServiceToken? serviceToken))
            return TryGetReferenceTokenFromParent(reference, out token);

        token = serviceToken;
        return true;
    }

    private bool TryGetReferenceTokenFromParent(object reference, [NotNullWhen(true)] out object? token)
    {
        if (parent is null)
        {
            token = null;
            return false;
        }

        return parent.TryGetReferenceToken(reference, out token);
    }
}
