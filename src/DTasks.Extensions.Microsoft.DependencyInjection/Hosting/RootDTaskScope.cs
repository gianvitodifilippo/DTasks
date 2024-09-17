namespace DTasks.Extensions.Microsoft.DependencyInjection.Hosting;

internal sealed class RootDTaskScope(IServiceProvider services, ServiceResolver resolver) : DTaskScope(services, resolver), IRootDTaskScope, IRootServiceMapper
{
    void IChildServiceMapper.MapService(object service, ServiceToken token) => MapService(service, token);
}
