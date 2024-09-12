namespace DTasks.Extensions.Microsoft.DependencyInjection.Hosting;

internal sealed class RootDTaskScope(IServiceProvider services, ServiceResolver resolver) : DTaskScope(services, resolver), IRootDTaskScope, IRootServiceMapper;
