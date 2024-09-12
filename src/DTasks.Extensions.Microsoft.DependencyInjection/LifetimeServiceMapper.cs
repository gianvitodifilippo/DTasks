using DTasks.Extensions.Microsoft.DependencyInjection.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

internal sealed class LifetimeServiceMapper(IServiceProvider applicationServices) : ILifetimeServiceMapper
{
    public object MapSingleton(IServiceProvider services, object service, ServiceToken token)
    {
        IRootServiceMapper marker = services.GetRequiredService<IRootServiceMapper>();
        marker.MapService(service, token);
        return service;
    }

    public object MapScoped(IServiceProvider services, object service, ServiceToken token)
    {
        IServiceMapper marker = services.GetRequiredService<IServiceMapper>();
        marker.MapService(service, token);
        return service;
    }

    public object MapTransient(IServiceProvider services, object service, ServiceToken token)
    {
        return ReferenceEquals(services, applicationServices)
            ? MapSingleton(services, service, token)
            : MapScoped(services, service, token);
    }
}
