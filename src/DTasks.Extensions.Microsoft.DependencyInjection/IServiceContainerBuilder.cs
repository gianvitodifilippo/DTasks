using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.Microsoft.DependencyInjection;

internal interface IServiceContainerBuilder
{
    void Replace(ServiceDescriptor descriptor);

    void AddDTaskServices();
}
