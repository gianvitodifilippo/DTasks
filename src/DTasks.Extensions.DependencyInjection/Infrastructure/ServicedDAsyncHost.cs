using System.Diagnostics.CodeAnalysis;
using DTasks.Extensions.DependencyInjection.Configuration;
using DTasks.Extensions.DependencyInjection.Infrastructure.Features;
using DTasks.Infrastructure;
using DTasks.Utils;

namespace DTasks.Extensions.DependencyInjection.Infrastructure;

public abstract class ServicedDAsyncHost : DAsyncHost, IServiceProviderFeature
{
    protected abstract IServiceProvider Services { get; }
    
    IServiceProvider IServiceProviderFeature.Services => Services;

    protected override bool TryGetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value)
    {
        if (key.Equals(InfrastructureServiceProvider.ServiceProviderKey))
        {
            value = (TProperty)Services;
            return true;
        }

        return base.TryGetProperty(key, out value);
    }

    protected override void OnInitialize(IDAsyncFlowInitializationContext context)
    {
        context.SetFeature<IServiceProviderFeature>(this);
    }

    public static ServicedDAsyncHost CreateDefault(IServiceProvider services)
    {
        ThrowHelper.ThrowIfNull(services);
        
        return new DefaultServicedDAsyncHost(services);
    }
    
    private sealed class DefaultServicedDAsyncHost(IServiceProvider services) : ServicedDAsyncHost
    {
        protected override IServiceProvider Services => services;
    }
}