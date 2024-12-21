using DTasks.Marshaling;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Frozen;
using System.Diagnostics;

namespace DTasks.Extensions.Microsoft.Extensions.DependencyInjection.Marshaling;

internal class ServiceProviderDAsyncMarshaler : IDAsyncMarshaler, ITokenConverter
{
    private static readonly FrozenSet<Type> s_tokenTypes = FrozenSet.ToFrozenSet([
        typeof(ServiceToken),
        typeof(KeyedServiceToken<string>),
        typeof(KeyedServiceToken<int>)]);

    private readonly IServiceProvider _provider;
    private readonly IDAsyncServiceRegister _register;
    private readonly ITypeResolver _typeResolver;
    private readonly Dictionary<object, ServiceToken> _tokens;

    protected ServiceProviderDAsyncMarshaler(
        IServiceProvider provider,
        IDAsyncServiceRegister register,
        ITypeResolver typeResolver)
    {
        _provider = provider;
        _register = register;
        _typeResolver = typeResolver;
        _tokens = [];
    }

    protected void MapService(object service, ServiceToken token)
    {
        _tokens.Add(service, token);
    }

    bool IDAsyncMarshaler.TryMarshal<T, TAction>(in T value, scoped ref TAction action)
    {
        return TryMarshal(in value, ref action);
    }

    bool IDAsyncMarshaler.TryUnmarshal<T, TAction>(TypeId typeId, scoped ref TAction action)
    {
        return TryUnmarshal<T, TAction>(typeId, ref action);
    }

    protected virtual bool TryMarshal<T, TAction>(in T value, scoped ref TAction action)
        where TAction : struct, IMarshalingAction
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        if (typeof(T).IsValueType || value is null || !_tokens.TryGetValue(value, out ServiceToken? token))
            return false;

        TypeId typeId;
        switch (token)
        {
            case KeyedServiceToken<string> keyedToken:
                typeId = _typeResolver.GetTypeId(typeof(KeyedServiceToken<string>));
                action.MarshalAs(typeId, keyedToken);
                break;

            case KeyedServiceToken<int> keyedToken:
                typeId = _typeResolver.GetTypeId(typeof(KeyedServiceToken<int>));
                action.MarshalAs(typeId, keyedToken);
                break;

            default:
                Debug.Assert(token.GetType() == typeof(ServiceToken), $"Unexpected token of type '{token.GetType().Name}'.");

                typeId = _typeResolver.GetTypeId(typeof(ServiceToken));
                action.MarshalAs(typeId, token);
                break;
        }

        return true;
    }

    private bool TryUnmarshal<T, TAction>(TypeId typeId, scoped ref TAction action)
        where TAction : struct, IUnmarshalingAction
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        Type tokenType = _typeResolver.GetType(typeId);

        if (!s_tokenTypes.Contains(tokenType))
            return false;

        action.UnmarshalAs(tokenType, this);
        return true;
    }

    public T Convert<TToken, T>(TToken token)
    {
        if (token is not ServiceToken serviceToken || !_register.IsDAsyncService(serviceToken.TypeId, out Type? serviceType))
            throw new ArgumentException("Invalid token.", nameof(token));

        object service = serviceToken is IKeyedServiceToken { Key: var serviceKey }
            ? _provider.GetRequiredKeyedService(serviceType, serviceKey)
            : _provider.GetRequiredService(serviceType);

        return service is not T value
            ? throw new InvalidOperationException("The token is not compatible with the required value type.")
            : value;
    }
}
