using DTasks.Extensions.Microsoft.DependencyInjection.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.Microsoft.DependencyInjection.Mapping;

internal sealed class ServiceMapper(IServiceProvider rootProvider) : IServiceMapper
{
    public object MapSingleton(IServiceProvider provider, object service, ServiceToken token)
    {
        IRootServiceMapper mapper = provider.GetRequiredService<IRootServiceMapper>();
        mapper.MapService(service, token);
        return service;
    }

    public object MapScoped(IServiceProvider provider, object service, ServiceToken token)
    {
        IChildServiceMapper mapper = provider.GetRequiredService<IChildServiceMapper>();
        mapper.MapService(service, token);
        return service;
    }

    public object MapTransient(IServiceProvider provider, object service, ServiceToken token)
    {
        return ReferenceEquals(provider, rootProvider)
            ? MapSingleton(provider, service, token)
            : MapScoped(provider, service, token);
    }
}
