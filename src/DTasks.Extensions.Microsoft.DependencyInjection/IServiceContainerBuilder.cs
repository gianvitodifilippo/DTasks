using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

internal interface IServiceContainerBuilder
{
    void Intercept(ServiceDescriptor descriptor);

    void AddDTaskServices();
}