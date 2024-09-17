using DTasks.Extensions.Microsoft.DependencyInjection.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

internal sealed class ServiceMapper(IServiceProvider applicationServices) : IServiceMapper
{
    public object MapSingleton(IServiceProvider services, object service, ServiceToken token)
    {
        IRootServiceMapper mapper = services.GetRequiredService<IRootServiceMapper>();
        mapper.MapService(service, token);
        return service;
    }

    public object MapScoped(IServiceProvider services, object service, ServiceToken token)
    {
        IChildServiceMapper mapper = services.GetRequiredService<IChildServiceMapper>();
        mapper.MapService(service, token);
        return service;
    }

    public object MapTransient(IServiceProvider services, object service, ServiceToken token)
    {
        return ReferenceEquals(services, applicationServices)
            ? MapSingleton(services, service, token)
            : MapScoped(services, service, token);
    }
}
