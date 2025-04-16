using DTasks.Extensions.DependencyInjection.Marshaling;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection.Mapping;

internal sealed class ServiceMapper(IServiceProvider rootProvider) : IServiceMapper
{
    public object MapSingleton(IServiceProvider provider, object service, ServiceSurrogate surrogate)
    {
        IRootServiceMapper mapper = provider.GetRequiredService<IRootServiceMapper>();
        mapper.MapService(service, surrogate);
        return service;
    }

    public object MapScoped(IServiceProvider provider, object service, ServiceSurrogate surrogate)
    {
        IChildServiceMapper mapper = provider.GetRequiredService<IChildServiceMapper>();
        mapper.MapService(service, surrogate);
        return service;
    }

    public object MapTransient(IServiceProvider provider, object service, ServiceSurrogate surrogate)
    {
        return ReferenceEquals(provider, rootProvider)
            ? MapSingleton(provider, service, surrogate)
            : MapScoped(provider, service, surrogate);
    }
}
