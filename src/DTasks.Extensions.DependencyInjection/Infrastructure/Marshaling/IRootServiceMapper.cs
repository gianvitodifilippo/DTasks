namespace DTasks.Extensions.DependencyInjection.Infrastructure.Marshaling;

internal interface IRootServiceMapper
{
    void MapService(object service, ServiceSurrogate surrogate);
}
