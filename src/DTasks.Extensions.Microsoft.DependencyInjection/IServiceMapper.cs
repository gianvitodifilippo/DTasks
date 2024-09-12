using DTasks.Extensions.Microsoft.DependencyInjection.Hosting;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

internal interface IServiceMapper
{
    void MapService(object service, ServiceToken token);
}
