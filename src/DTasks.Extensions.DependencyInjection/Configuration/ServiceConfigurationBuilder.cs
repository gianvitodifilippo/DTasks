using DTasks.Configuration;
using DTasks.Infrastructure.Marshaling;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection.Configuration;

using KeyedServiceIdentifier = (Type ServiceType, object? ServiceKey);

internal sealed class ServiceConfigurationBuilder : IServiceConfigurationBuilder
{
    private readonly HashSet<KeyedServiceIdentifier> _keyedServiceIdentifiers = [];
    private readonly HashSet<Type> _serviceIdentifiers = [];
    private readonly HashSet<ISurrogatableTypeContext> _surrogatableTypes = [];

    public bool IsDAsyncService(ServiceDescriptor descriptor)
    {
        Type serviceType = descriptor.ServiceType;

        return
            descriptor.IsKeyedService && _keyedServiceIdentifiers.Contains((serviceType, descriptor.ServiceKey)) ||
            _serviceIdentifiers.Contains(serviceType);
    }

    public void ConfigureMarshaling(IMarshalingConfigurationBuilder marshaling)
    {
        RegisterSurrogatableTypeAction action = new(marshaling);
        foreach (ISurrogatableTypeContext surrogatableTypeContext in _surrogatableTypes)
        {
            surrogatableTypeContext.Execute(ref action);
        }
    }

    IServiceConfigurationBuilder IServiceConfigurationBuilder.RegisterDAsyncService<TService>()
    {
        RegisterDAsyncService<TService, TService>();
        return this;
    }

    IServiceConfigurationBuilder IServiceConfigurationBuilder.RegisterDAsyncService<TService>(object? serviceKey)
    {
        RegisterDAsyncKeyedService<TService, TService>(serviceKey);
        return this;
    }

    IServiceConfigurationBuilder IServiceConfigurationBuilder.RegisterDAsyncService<TService, TImplementation>()
    {
        RegisterDAsyncService<TService, TImplementation>();
        return this;
    }

    IServiceConfigurationBuilder IServiceConfigurationBuilder.RegisterDAsyncService<TService, TImplementation>(object? serviceKey)
    {
        RegisterDAsyncKeyedService<TService, TImplementation>(serviceKey);
        return this;
    }

    private void RegisterDAsyncService<TService, TImplementation>()
    {
        _serviceIdentifiers.Add(typeof(TService));
        _surrogatableTypes.Add(SurrogatableTypeContext.Of<TImplementation>());
    }

    private void RegisterDAsyncKeyedService<TService, TImplementation>(object? serviceKey)
    {
        if (serviceKey is null)
        {
            _serviceIdentifiers.Add(typeof(TService));
        }
        else
        {
            _keyedServiceIdentifiers.Add((typeof(TService), serviceKey));   
        }
        
        _surrogatableTypes.Add(SurrogatableTypeContext.Of<TImplementation>());
    }
    
    private readonly struct RegisterSurrogatableTypeAction(IMarshalingConfigurationBuilder marshaling) : ISurrogatableTypeAction
    {
        public void Invoke<TSurrogatable>() => marshaling.RegisterSurrogatableType<TSurrogatable>();
    }
}