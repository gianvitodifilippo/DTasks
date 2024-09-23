using DTasks.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Extensions.Microsoft.DependencyInjection.Hosting;

internal class DTaskScope : IDTaskScope
{
    private readonly IServiceProvider _provider;
    private readonly IDAsyncServiceRegister _register;
    private readonly Dictionary<object, ServiceToken> _tokens;

    protected DTaskScope(IServiceProvider provider, IDAsyncServiceRegister register)
    {
        _provider = provider;
        _register = register;
        _tokens = [];
    }

    protected void MapService(object service, ServiceToken token)
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

        if (!_register.IsDAsyncService(typeId, out Type? serviceType))
            return False(out reference);

        reference = serviceToken is IKeyedServiceToken { Key: var serviceKey }
            ? _provider.GetRequiredKeyedService(serviceType, serviceKey)
            : _provider.GetRequiredService(serviceType);

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
