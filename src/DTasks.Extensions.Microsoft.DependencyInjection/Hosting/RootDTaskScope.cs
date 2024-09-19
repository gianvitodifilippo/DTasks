namespace DTasks.Extensions.Microsoft.DependencyInjection.Hosting;

internal sealed class RootDTaskScope(IServiceProvider provider, IServiceRegister register) : DTaskScope(provider, register), IRootDTaskScope, IRootServiceMapper
{
    void IChildServiceMapper.MapService(object service, ServiceToken token) => MapService(service, token);
}
