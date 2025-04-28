using DTasks.Configuration.DependencyInjection;

namespace DTasks.Infrastructure;

internal interface IDAsyncScope : IDAsyncFlow
{
    TComponent GetScopedComponent<TComponent>(IComponentDescriptor<TComponent> descriptor, FlowImplementationFactory<TComponent> createComponent)
        where TComponent : notnull;

    TComponent GetBoundComponent<TComponent, TDependency>(
        IComponentProvider<TDependency> dependencyProvider,
        IComponentDescriptor<TComponent> descriptor,
        DescriptorResolver<TComponent, TDependency> resolve)
        where TComponent : notnull
        where TDependency : notnull;
}