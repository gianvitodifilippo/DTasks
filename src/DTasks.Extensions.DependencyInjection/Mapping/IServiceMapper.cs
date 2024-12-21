using DTasks.Extensions.DependencyInjection.Marshaling;

namespace DTasks.Extensions.DependencyInjection.Mapping;

internal interface IServiceMapper
{
    object MapScoped(IServiceProvider provider, object service, ServiceToken token);

    object MapSingleton(IServiceProvider provider, object service, ServiceToken token);

    object MapTransient(IServiceProvider provider, object service, ServiceToken token);
}
