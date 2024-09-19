using DTasks.Extensions.Microsoft.DependencyInjection.Hosting;

namespace DTasks.Extensions.Microsoft.DependencyInjection.Mapping;

internal interface IChildServiceMapper
{
    void MapService(object service, ServiceToken token);
}
