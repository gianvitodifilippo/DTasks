using DTasks.Extensions.Microsoft.DependencyInjection.Hosting;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

internal interface IServiceMapper
{
    object MapScoped(IServiceProvider provider, object service, ServiceToken token);

    object MapSingleton(IServiceProvider provider, object service, ServiceToken token);
    
    object MapTransient(IServiceProvider provider, object service, ServiceToken token);
}
