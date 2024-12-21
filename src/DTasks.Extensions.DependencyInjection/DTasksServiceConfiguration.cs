using DTasks.Marshaling;
using DTasks.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace DTasks.Extensions.DependencyInjection;

using KeyedServiceIdentifier = (Type ServiceType, object? ServiceKey);

internal sealed class DTasksServiceConfiguration(IServiceCollection services) : IDTasksServiceConfiguration
{
    private readonly HashSet<KeyedServiceIdentifier> _additionalKeyedServiceTypes = [];
    private readonly HashSet<Type> _additionalServiceTypes = [];
    private ITypeResolverBuilder? _typeResolverBuilder;

    public IServiceCollection Services => services;

    public ServiceContainerBuilder CreateContainerBuilder()
    {
        ITypeResolverBuilder typeResolverBuilder = _typeResolverBuilder ?? TypeResolverBuilder.CreateDefault();
        return new ServiceContainerBuilder(services, typeResolverBuilder, new DAsyncServiceRegisterBuilder(typeResolverBuilder));
    }

    public IDTasksServiceConfiguration UseTypeResolverBuilder(ITypeResolverBuilder typeResolverBuilder)
    {
        _typeResolverBuilder = typeResolverBuilder;
        return this;
    }

    public IDTasksServiceConfiguration RegisterDAsyncService(Type serviceType)
    {
        ThrowHelper.ThrowIfNull(serviceType);

        return RegisterDAsyncServiceCore(serviceType);
    }

    public IDTasksServiceConfiguration RegisterDAsyncService(Type serviceType, object? serviceKey)
    {
        ThrowHelper.ThrowIfNull(serviceType);

        return serviceKey is null
            ? RegisterDAsyncServiceCore(serviceType)
            : RegisterDAsyncServiceCore(serviceType, serviceKey);
    }

    private IDTasksServiceConfiguration RegisterDAsyncServiceCore(Type serviceType)
    {
        if (serviceType.ContainsGenericParameters)
            throw OpenGenericsNotSupported();

        if (!services.Any(descriptor => descriptor.ServiceType == serviceType))
            throw new ArgumentException($"Type '{serviceType.Name}' was not registered as a service.", nameof(serviceType));

        _additionalServiceTypes.Add(serviceType);
        return this;
    }

    private IDTasksServiceConfiguration RegisterDAsyncServiceCore(Type serviceType, object serviceKey)
    {
        if (serviceType.ContainsGenericParameters)
            throw OpenGenericsNotSupported();

        if (!services.Any(descriptor => descriptor.ServiceType == serviceType && Equals(descriptor.ServiceKey, serviceKey)))
            throw new ArgumentException($"Type '{serviceType.Name}' was not registered as a service with key '{serviceKey}'.", nameof(serviceType));

        _additionalKeyedServiceTypes.Add((serviceType, serviceKey));
        return this;
    }

    public void ReplaceDAsyncServices(IServiceContainerBuilder containerBuilder)
    {
        List<ServiceDescriptor> toReplace = services.Where(IsDAsyncService).ToList();
        if (toReplace.Any(descriptor => descriptor.ServiceType.ContainsGenericParameters))
            throw OpenGenericsNotSupported();

        foreach (ServiceDescriptor descriptor in toReplace)
        {
            containerBuilder.Replace(descriptor);
        }
    }

    private bool IsDAsyncService(ServiceDescriptor descriptor)
    {
        Type serviceType = descriptor.ServiceType;

        if (descriptor.IsKeyedService && _additionalKeyedServiceTypes.Contains((serviceType, descriptor.ServiceKey)))
            return true;

        if (_additionalServiceTypes.Contains(serviceType))
            return true;

        return serviceType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Any(method => typeof(DTask).IsAssignableFrom(method.ReturnType) && !method.IsDefined(typeof(NonDAsyncAttribute)));
    }

    private static NotSupportedException OpenGenericsNotSupported() => new("Usage of open generic services within d-async flows is not supported.");
}

