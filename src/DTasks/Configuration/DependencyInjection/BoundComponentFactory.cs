using System.Diagnostics;
using DTasks.Infrastructure;

namespace DTasks.Configuration.DependencyInjection;

internal static class BoundComponentFactory
{
    public static TComponent CreateComponent<TComponent, TDependency>(
        IComponentProvider<TDependency> dependencyProvider,
        IComponentDescriptor<TComponent> descriptor,
        DescriptorResolver<TComponent, TDependency> resolve,
        IDAsyncScope scope)
        where TComponent : notnull
        where TDependency : notnull
    {
        TDependency dependency = dependencyProvider.GetComponent(scope);
        IComponentDescriptor<TComponent> resolvedDescriptor = resolve(dependency);

        return resolvedDescriptor.Build(new Builder<TComponent>(descriptor, scope));
    }

    [method: DebuggerStepThrough]
    private sealed class Builder<TComponent>(
        IComponentDescriptor<TComponent> descriptor,
        IDAsyncScope scope) : IDAsyncInfrastructureBuilder<TComponent, TComponent>
        where TComponent : notnull
    {
        public TComponent Unit(TComponent component)
        {
            scope.AddComponent(descriptor, component);
            return component;
        }

        public TComponent Singleton(ConfiguredImplementationFactory<TComponent> createComponent)
        {
            TComponent component = createComponent(scope.Configuration);
            scope.AddComponent(descriptor, component);
            return component;
        }

        public TComponent Scoped(FlowImplementationFactory<TComponent> createComponent)
        {
            TComponent component = createComponent(scope);
            scope.AddComponent(descriptor, component);
            return component;
        }

        public TComponent RootTransient(ConfiguredImplementationFactory<TComponent> createComponent)
        {
            return createComponent(scope.Configuration);
        }

        public TComponent FlowTransient(FlowImplementationFactory<TComponent> createComponent)
        {
            return createComponent(scope);
        }

        public TComponent Bind<TDependency>(IComponentDescriptor<TDependency> dependencyDescriptor, DescriptorResolver<TComponent, TDependency> resolve)
            where TDependency : notnull
        {
            IComponentProvider<TDependency> dependencyProvider = ComponentProviderFactory.CreateProvider(scope.Configuration, dependencyDescriptor);
            return CreateComponent(dependencyProvider, descriptor, resolve, scope);
        }
    }
}