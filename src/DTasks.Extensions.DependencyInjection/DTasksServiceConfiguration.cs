using DTasks.Extensions.DependencyInjection.Marshaling;
using DTasks.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Extensions.DependencyInjection;

using KeyedServiceIdentifier = (Type ServiceType, object? ServiceKey);

internal sealed class DTasksServiceConfiguration(IServiceCollection services) : IDTasksServiceConfiguration
{
    private readonly HashSet<KeyedServiceIdentifier> _additionalKeyedServiceTypes = [];
    private readonly HashSet<Type> _additionalServiceTypes = [];
    private Action<IDAsyncTypeResolverBuilder>? _configureTypeResolver;
    private IDAsyncTypeResolverBuilder? _typeResolverBuilder;

    public ServiceContainerBuilder CreateContainerBuilder()
    {
        IDAsyncTypeResolverBuilder typeResolverBuilder = _typeResolverBuilder ?? DAsyncTypeResolverBuilder.CreateDefault();
        _configureTypeResolver?.Invoke(typeResolverBuilder);
        typeResolverBuilder.Register(typeof(ServiceToken));
        typeResolverBuilder.Register(typeof(KeyedServiceToken<string>));
        typeResolverBuilder.Register(typeof(KeyedServiceToken<int>));

        return new ServiceContainerBuilder(services, typeResolverBuilder, new DAsyncServiceRegisterBuilder(typeResolverBuilder));
    }

    public IDTasksServiceConfiguration UseTypeResolverBuilder(IDAsyncTypeResolverBuilder typeResolverBuilder)
    {
        ThrowHelper.ThrowIfNull(typeResolverBuilder);

        _typeResolverBuilder = typeResolverBuilder;
        return this;
    }

    public IDTasksServiceConfiguration ConfigureTypeResolver(Action<IDAsyncTypeResolverBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(configure);

        _configureTypeResolver = configure;
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

    private DTasksServiceConfiguration RegisterDAsyncServiceCore(Type serviceType)
    {
        if (serviceType.ContainsGenericParameters)
            throw OpenGenericsNotSupported();

        if (!services.Any(descriptor => descriptor.ServiceType == serviceType))
            throw new ArgumentException($"Type '{serviceType.Name}' was not registered as a service.", nameof(serviceType));

        _additionalServiceTypes.Add(serviceType);
        return this;
    }

    private DTasksServiceConfiguration RegisterDAsyncServiceCore(Type serviceType, object serviceKey)
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
            .Any(method => typeof(DTask).IsAssignableFrom(method.ReturnType));
    }

    private static NotSupportedException OpenGenericsNotSupported() => new("Usage of open generic services within d-async flows is not supported.");
}
