using DTasks.Extensions.Microsoft.DependencyInjection.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

internal sealed class ServiceMarker(IServiceProvider applicationServices, object rootScopeKey, object childScopeKey)
{
    public object MarkSingleton(IServiceProvider services, object instance, ServiceToken token)
    {
        DTaskScope scope = services.GetRequiredKeyedService<DTaskScope>(rootScopeKey);
        scope.MarkService(instance, token);
        return instance;
    }

    public object MarkScoped(IServiceProvider services, object instance, ServiceToken token)
    {
        DTaskScope scope = services.GetRequiredKeyedService<DTaskScope>(childScopeKey);
        scope.MarkService(instance, token);
        return instance;
    }

    public object MarkTransient(IServiceProvider services, object instance, ServiceToken token)
    {
        return ReferenceEquals(services, applicationServices)
            ? MarkSingleton(services, instance, token)
            : MarkScoped(services, instance, token);
    }
}
