using DTasks.Extensions.DependencyInjection.Marshaling;

namespace DTasks.Extensions.DependencyInjection.Mapping;

internal interface IChildServiceMapper
{
    void MapService(object service, ServiceToken token);
}
