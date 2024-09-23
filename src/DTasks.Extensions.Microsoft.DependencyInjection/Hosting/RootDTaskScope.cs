using DTasks.Extensions.Microsoft.DependencyInjection.Mapping;

namespace DTasks.Extensions.Microsoft.DependencyInjection.Hosting;

internal sealed class RootDTaskScope(IServiceProvider provider, IDAsyncServiceRegister register) : DTaskScope(provider, register), IRootDTaskScope, IRootServiceMapper
{
    public new void MapService(object service, ServiceToken token) => base.MapService(service, token);
}
