using DTasks.Extensions.Microsoft.DependencyInjection.Hosting;

namespace DTasks.Extensions.Microsoft.DependencyInjection;
internal interface ILifetimeServiceMapper
{
    object MapScoped(IServiceProvider services, object service, ServiceToken token);
    object MapSingleton(IServiceProvider services, object service, ServiceToken token);
    object MapTransient(IServiceProvider services, object service, ServiceToken token);
}