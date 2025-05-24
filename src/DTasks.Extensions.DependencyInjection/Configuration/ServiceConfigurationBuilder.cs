using DTasks.Configuration;
using DTasks.Infrastructure.Generics;
using DTasks.Infrastructure.Marshaling;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection.Configuration;

using KeyedServiceIdentifier = (Type ServiceType, object? ServiceKey);

internal sealed class ServiceConfigurationBuilder : IServiceConfigurationBuilder
{
    private readonly HashSet<KeyedServiceIdentifier> _keyedServiceIdentifiers = [];
    private readonly HashSet<Type> _serviceIdentifiers = [];
    private readonly HashSet<ITypeContext> _surrogatableTypeContexts = [];

    public bool IsDAsyncService(ServiceDescriptor descriptor)
    {
        Type serviceType = descriptor.ServiceType;

        return
            descriptor.IsKeyedService && _keyedServiceIdentifiers.Contains((serviceType, descriptor.ServiceKey)) ||
            _serviceIdentifiers.Contains(serviceType);
    }

    public void ConfigureMarshaling(IMarshalingConfigurationBuilder marshaling)
    {
        foreach (ITypeContext typeContext in _surrogatableTypeContexts)
        {
            marshaling.RegisterSurrogatableType(typeContext);
        }
    }

    IServiceConfigurationBuilder IServiceConfigurationBuilder.RegisterDAsyncService<TService>()
    {
        RegisterDAsyncService<TService>(null);
        return this;
    }

    IServiceConfigurationBuilder IServiceConfigurationBuilder.RegisterDAsyncService<TService>(object? serviceKey)
    {
        RegisterDAsyncService<TService>(serviceKey);
        return this;
    }

    private void RegisterDAsyncService<TService>(object? serviceKey)
    {
        if (serviceKey is null)
        {
            _serviceIdentifiers.Add(typeof(TService));
        }
        else
        {
            _keyedServiceIdentifiers.Add((typeof(TService), serviceKey));   
        }
        
        _surrogatableTypeContexts.Add(TypeContext.Of<TService>());
    }
}