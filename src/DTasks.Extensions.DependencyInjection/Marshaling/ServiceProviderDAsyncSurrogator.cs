using Microsoft.Extensions.DependencyInjection;
using System.Collections.Frozen;
using System.Diagnostics;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Extensions.DependencyInjection.Marshaling;

internal class ServiceProviderDAsyncSurrogator : IDAsyncSurrogator, ISurrogateConverter
{
    private static readonly FrozenSet<Type> s_surrogateTypes = FrozenSet.ToFrozenSet([
        typeof(ServiceSurrogate),
        typeof(KeyedServiceSurrogate<string>),
        typeof(KeyedServiceSurrogate<int>)]);

    private readonly IServiceProvider _provider;
    private readonly IDAsyncServiceRegister _register;
    private readonly IDAsyncTypeResolver _typeResolver;
    private readonly Dictionary<object, ServiceSurrogate> _surrogates;

    protected ServiceProviderDAsyncSurrogator(
        IServiceProvider provider,
        IDAsyncServiceRegister register,
        IDAsyncTypeResolver typeResolver)
    {
        _provider = provider;
        _register = register;
        _typeResolver = typeResolver;
        _surrogates = [];
    }

    protected void MapService(object service, ServiceSurrogate surrogate)
    {
        _surrogates.Add(service, surrogate);
    }

    bool IDAsyncSurrogator.TrySurrogate<T, TAction>(in T value, scoped ref TAction action)
    {
        return TrySurrogate(in value, ref action);
    }

    bool IDAsyncSurrogator.TryRestore<T, TAction>(TypeId typeId, scoped ref TAction action)
    {
        return TryRestore<T, TAction>(typeId, ref action);
    }

    protected virtual bool TrySurrogate<T, TAction>(in T value, scoped ref TAction action)
        where TAction : struct, ISurrogationAction
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        if (typeof(T).IsValueType || value is null || !_surrogates.TryGetValue(value, out ServiceSurrogate? surrogate))
            return false;

        TypeId typeId;
        switch (surrogate)
        {
            case KeyedServiceSurrogate<string> keyedSurrogate:
                typeId = _typeResolver.GetTypeId(typeof(KeyedServiceSurrogate<string>));
                action.SurrogateAs(typeId, keyedSurrogate);
                break;

            case KeyedServiceSurrogate<int> keyedSurrogate:
                typeId = _typeResolver.GetTypeId(typeof(KeyedServiceSurrogate<int>));
                action.SurrogateAs(typeId, keyedSurrogate);
                break;

            default:
                Debug.Assert(surrogate.GetType() == typeof(ServiceSurrogate), $"Unexpected surrogate of type '{surrogate.GetType().Name}'.");

                typeId = _typeResolver.GetTypeId(typeof(ServiceSurrogate));
                action.SurrogateAs(typeId, surrogate);
                break;
        }

        return true;
    }

    private bool TryRestore<T, TAction>(TypeId typeId, scoped ref TAction action)
        where TAction : struct, IRestorationAction
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        Type surrogateType = _typeResolver.GetType(typeId);

        if (!s_surrogateTypes.Contains(surrogateType))
            return false;

        action.RestoreAs(surrogateType, this);
        return true;
    }

    public T Convert<TSurrogate, T>(TSurrogate surrogate)
    {
        if (surrogate is not ServiceSurrogate serviceToken || !_register.IsDAsyncService(serviceToken.TypeId, out Type? serviceType))
            throw new ArgumentException("Invalid surrogate.", nameof(surrogate));

        object service = serviceToken is IKeyedServiceToken { Key: var serviceKey }
            ? _provider.GetRequiredKeyedService(serviceType, serviceKey)
            : _provider.GetRequiredService(serviceType);

        return service is not T value
            ? throw new InvalidOperationException("The surrogate is not compatible with the required value type.")
            : value;
    }
}
