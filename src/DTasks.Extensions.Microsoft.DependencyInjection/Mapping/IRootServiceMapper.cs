using DTasks.Extensions.Microsoft.DependencyInjection.Hosting;

namespace DTasks.Extensions.Microsoft.DependencyInjection.Mapping;

internal interface IRootServiceMapper
{
    void MapService(object service, ServiceToken token);
}
