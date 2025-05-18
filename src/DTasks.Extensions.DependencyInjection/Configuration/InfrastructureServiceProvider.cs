using DTasks.Configuration.DependencyInjection;
using DTasks.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection.Configuration;

public static class InfrastructureServiceProvider
{
    internal static readonly DAsyncPropertyKey<IServiceProvider> ServiceProviderKey = new();

    public static readonly IComponentDescriptor<IServiceProvider> Descriptor = ComponentDescriptor.Host(host => host.GetRequiredProperty(ServiceProviderKey));

    public static IComponentDescriptor<TService> GetRequiredService<TService>()
        where TService : notnull
    {
        return ComponentDescriptor.Host(host => host
            .GetRequiredProperty(ServiceProviderKey)
            .GetRequiredService<TService>());
    }

    public static IComponentDescriptor<TService> GetRequiredKeyedService<TService>(object serviceKey)
        where TService : notnull
    {
        return ComponentDescriptor.Host(host => host
            .GetRequiredProperty(ServiceProviderKey)
            .GetRequiredKeyedService<TService>(serviceKey));
    }
}