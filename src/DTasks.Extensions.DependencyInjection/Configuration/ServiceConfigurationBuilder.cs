using DTasks.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection.Configuration;

using KeyedServiceIdentifier = (Type ServiceType, object? ServiceKey);

internal sealed class ServiceConfigurationBuilder : IServiceConfigurationBuilder
{
    private readonly HashSet<KeyedServiceIdentifier> _additionalKeyedServiceTypes = [];
    private readonly HashSet<Type> _additionalServiceTypes = [];
    private bool _replaceAllServices;

    public bool IsDAsyncService(ServiceDescriptor descriptor)
    {
        Type serviceType = descriptor.ServiceType;

        if (_replaceAllServices)
            return IsSupportedServiceType(serviceType);

        return
            descriptor.IsKeyedService && _additionalKeyedServiceTypes.Contains((serviceType, descriptor.ServiceKey)) ||
            _additionalServiceTypes.Contains(serviceType);
    }

    IServiceConfigurationBuilder IServiceConfigurationBuilder.RegisterAllServices(bool registerAll)
    {
        _replaceAllServices = registerAll;
        return this;
    }

    IServiceConfigurationBuilder IServiceConfigurationBuilder.RegisterDAsyncService(Type serviceType)
    {
        ThrowHelper.ThrowIfNull(serviceType);

        return RegisterDAsyncServiceCore(serviceType);
    }

    IServiceConfigurationBuilder IServiceConfigurationBuilder.RegisterDAsyncService(Type serviceType, object? serviceKey)
    {
        ThrowHelper.ThrowIfNull(serviceType);

        return serviceKey is null
            ? RegisterDAsyncServiceCore(serviceType)
            : RegisterDAsyncServiceCore(serviceType, serviceKey);
    }

    private IServiceConfigurationBuilder RegisterDAsyncServiceCore(Type serviceType)
    {
        if (!IsSupportedServiceType(serviceType))
            throw UnsupportedServiceType();

        _additionalServiceTypes.Add(serviceType);
        return this;
    }

    private IServiceConfigurationBuilder RegisterDAsyncServiceCore(Type serviceType, object serviceKey)
    {
        if (!IsSupportedServiceType(serviceType))
            throw UnsupportedServiceType();

        _additionalKeyedServiceTypes.Add((serviceType, serviceKey));
        return this;
    }

    private static bool IsSupportedServiceType(Type serviceType) => !serviceType.ContainsGenericParameters && !serviceType.IsGenericTypeDefinition;

    private static NotSupportedException UnsupportedServiceType() => new("Open generic types and types containing generic type parameters are not supported.");
}