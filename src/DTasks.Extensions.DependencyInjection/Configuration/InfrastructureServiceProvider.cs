using DTasks.Configuration.DependencyInjection;
using DTasks.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection.Configuration;

public static class InfrastructureServiceProvider
{
    internal static readonly DAsyncFlowPropertyKey<IServiceProvider> ServiceProviderKey = new();

    public static readonly IComponentDescriptor<IServiceProvider> ServicesProvider = ComponentDescriptor.Scoped(flow => flow.GetProperty(ServiceProviderKey));

    public static IComponentDescriptor<TService> GetRequiredService<TService>()
        where TService : notnull
    {
        return ServicesProvider.Map(services => services.GetRequiredService<TService>());
    }

    public static IComponentDescriptor<TService> GetRequiredKeyedService<TService>(object serviceKey)
        where TService : notnull
    {
        return ServicesProvider.Map(services => services.GetRequiredKeyedService<TService>(serviceKey));
    }
}