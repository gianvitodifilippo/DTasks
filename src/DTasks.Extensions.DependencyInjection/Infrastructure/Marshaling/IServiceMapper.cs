namespace DTasks.Extensions.DependencyInjection.Infrastructure.Marshaling;

internal interface IServiceMapper
{
    object MapScoped(IServiceProvider provider, object service, ServiceSurrogate surrogate);

    object MapSingleton(IServiceProvider provider, object service, ServiceSurrogate surrogate);

    object MapTransient(IServiceProvider provider, object service, ServiceSurrogate surrogate);
}
