using DTasks.Extensions.Microsoft.Extensions.DependencyInjection.Marshaling;

namespace DTasks.Extensions.Microsoft.Extensions.DependencyInjection.Mapping;

internal interface IChildServiceMapper
{
    void MapService(object service, ServiceToken token);
}
