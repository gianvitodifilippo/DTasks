using DTasks.Configuration.DependencyInjection;

namespace DTasks.Infrastructure.DependencyInjection;

internal interface IComponentProviderBuilder
{
    InfrastructureComponentToken<TComponent> GetTokenInRootScope<TComponent>(IComponentDescriptor<TComponent> descriptor);
    
    InfrastructureComponentToken<TComponent> GetTokenInHostScope<TComponent>(IComponentDescriptor<TComponent> descriptor);
    
    InfrastructureComponentToken<TComponent> GetTokenInFlowScope<TComponent>(IComponentDescriptor<TComponent> descriptor);
}