using System.ComponentModel;

namespace DTasks.Configuration.DependencyInjection;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncInfrastructureBuilder<in TComponent, out TReturn>
    where TComponent : notnull
{
    TReturn Unit(TComponent component);

    TReturn Singleton(ConfiguredImplementationFactory<TComponent> createComponent);
    
    TReturn Scoped(FlowImplementationFactory<TComponent> createComponent);
    
    TReturn RootTransient(ConfiguredImplementationFactory<TComponent> createComponent);

    TReturn FlowTransient(FlowImplementationFactory<TComponent> createComponent);

    TReturn Bind<TDependency>(
        IComponentDescriptor<TDependency> dependencyDescriptor,
        DescriptorResolver<TComponent, TDependency> resolve)
        where TDependency : notnull;
}