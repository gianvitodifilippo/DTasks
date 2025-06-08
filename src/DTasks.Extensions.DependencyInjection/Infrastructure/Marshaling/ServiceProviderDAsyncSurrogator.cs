using System.Collections.Frozen;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DTasks.Infrastructure.Marshaling;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection.Infrastructure.Marshaling;

internal class ServiceProviderDAsyncSurrogator : IDAsyncSurrogator
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

    public virtual bool TrySurrogate<T, TMarshaller>(in T value, scoped ref TMarshaller marshaller)
        where TMarshaller : IMarshaller
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
                marshaller.WriteSurrogate(typeId, keyedSurrogate);
                break;

            case KeyedServiceSurrogate<int> keyedSurrogate:
                typeId = _typeResolver.GetTypeId(typeof(KeyedServiceSurrogate<int>));
                marshaller.WriteSurrogate(typeId, keyedSurrogate);
                break;

            default:
                Debug.Assert(surrogate.GetType() == typeof(ServiceSurrogate), $"Unexpected surrogate of type '{surrogate.GetType().Name}'.");

                typeId = _typeResolver.GetTypeId(typeof(ServiceSurrogate));
                marshaller.WriteSurrogate(typeId, surrogate);
                break;
        }

        return true;
    }

    public bool TryRestore<T, TUnmarshaller>(TypeId typeId, scoped ref TUnmarshaller unmarshaller, [MaybeNullWhen(false)] out T value)
        where TUnmarshaller : IUnmarshaller
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        Type surrogateType = _typeResolver.GetType(typeId);

        if (!s_surrogateTypes.Contains(surrogateType))
        {
            value = default;
            return false;
        }

        ServiceSurrogate surrogate = unmarshaller.ReadSurrogate<ServiceSurrogate>(surrogateType);
        if (!_register.IsDAsyncService(surrogate.TypeId, out Type? serviceType))
            throw new InvalidOperationException("Invalid surrogate.");

        object service = surrogate is IKeyedServiceSurrogate { Key: var serviceKey }
            ? _provider.GetRequiredKeyedService(serviceType, serviceKey)
            : _provider.GetRequiredService(serviceType);
        
        value = (T)service;
        return true;
    }
}
