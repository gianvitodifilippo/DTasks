
namespace DTasks.Extensions.Microsoft.DependencyInjection;

internal interface IServiceResolverBuilder
{
    ServiceTypeId AddServiceType(Type serviceType);
    
    ServiceResolver BuildServiceResolver();
}
