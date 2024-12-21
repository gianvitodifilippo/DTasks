using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection;

internal interface IServiceContainerBuilder
{
    void Replace(ServiceDescriptor descriptor);

    void AddDTaskServices();
}
