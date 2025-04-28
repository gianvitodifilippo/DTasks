namespace DTasks.Extensions.DependencyInjection.Infrastructure.Marshaling;

internal interface IChildServiceMapper
{
    void MapService(object service, ServiceSurrogate surrogate);
}
