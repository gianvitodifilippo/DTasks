using DTasks.Extensions.DependencyInjection.Marshaling;

namespace DTasks.Extensions.DependencyInjection.Mapping;

internal interface IRootServiceMapper
{
    void MapService(object service, ServiceToken token);
}
