
using System.Reflection;
using DTasks.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

using KeyedServiceIdentifier = (Type ServiceType, object? ServiceKey);

internal class DTasksServiceConfiguration(IServiceCollection services) : IDTasksServiceConfiguration
{
    private readonly HashSet<KeyedServiceIdentifier> _additionalKeyedServiceTypes = [];
    private readonly HashSet<Type> _additionalServiceTypes = [];

    public IServiceCollection Services => services;

    public IDTasksServiceConfiguration RegisterDAsyncService(Type serviceType, object? serviceKey)
    {
        ThrowHelper.ThrowIfNull(serviceType);

        if (serviceType.ContainsGenericParameters)
            throw OpenGenericsNotSupported();
        
        if (!services.Any(descriptor => descriptor.ServiceType == serviceType && Equals(descriptor.ServiceKey, serviceKey)))
            throw new ArgumentException($"Type '{serviceType.Name}' was not registered as a service.");

        _additionalKeyedServiceTypes.Add((serviceType, serviceKey));
        return this;
    }

    public IDTasksServiceConfiguration RegisterDAsyncService(Type serviceType)
    {
        ThrowHelper.ThrowIfNull(serviceType);

        if (serviceType.ContainsGenericParameters)
            throw OpenGenericsNotSupported();
        
        if (!services.Any(descriptor => descriptor.ServiceType == serviceType))
            throw new ArgumentException($"Type '{serviceType.Name}' was not registered as a service.");

        _additionalServiceTypes.Add(serviceType);
        return this;
    }

    internal void ReplaceDAsyncServices(IServiceContainerBuilder containerBuilder)
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
        if (descriptor.IsKeyedService && _additionalKeyedServiceTypes.Contains((descriptor.ServiceType, descriptor.ServiceKey)))
            return true;

        if (_additionalServiceTypes.Contains(descriptor.ServiceType))
            return true;

        return descriptor.ServiceType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Any(method => typeof(DTask).IsAssignableFrom(method.ReturnType));
    }

    private static NotSupportedException OpenGenericsNotSupported() => new("Usage of open generic services within d-async flows is not supported.");
}
