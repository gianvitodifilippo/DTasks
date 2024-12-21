using DTasks.Extensions.Microsoft.Extensions.DependencyInjection.Marshaling;

namespace DTasks.Extensions.Microsoft.Extensions.DependencyInjection.Mapping;

internal interface IServiceMapper
{
    object MapScoped(IServiceProvider provider, object service, ServiceToken token);

    object MapSingleton(IServiceProvider provider, object service, ServiceToken token);

    object MapTransient(IServiceProvider provider, object service, ServiceToken token);
}
