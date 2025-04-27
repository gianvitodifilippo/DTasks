namespace DTasks.Configuration.DependencyInjection;

internal static class ComponentProviderFactory
{
    // TODO: This becomes an internal instance method of DTasksConfiguration
    public static IComponentProvider<TComponent> CreateProvider<TComponent>(
        DTasksConfiguration configuration,
        IComponentDescriptor<TComponent> descriptor)
        where TComponent : notnull
    {
        return descriptor.Build(new Builder<TComponent>(configuration, descriptor));
    }

    private sealed class Builder<TComponent>(
        DTasksConfiguration configuration,
        IComponentDescriptor<TComponent> descriptor) : IDAsyncInfrastructureBuilder<TComponent, IComponentProvider<TComponent>>
        where TComponent : notnull
    {
        public IComponentProvider<TComponent> Unit(TComponent component)
        {
            return new SingletonComponentProvider<TComponent>(configuration, component);
        }

        public IComponentProvider<TComponent> Singleton(ConfiguredImplementationFactory<TComponent> createComponent)
        {
            return new SingletonComponentProvider<TComponent>(configuration, createComponent(configuration));
        }

        public IComponentProvider<TComponent> Scoped(FlowImplementationFactory<TComponent> createComponent)
        {
            return new ScopedComponentProvider<TComponent>(descriptor, createComponent);
        }

        public IComponentProvider<TComponent> RootTransient(ConfiguredImplementationFactory<TComponent> createComponent)
        {
            return new RootTransientComponentProvider<TComponent>(configuration, createComponent);
        }

        public IComponentProvider<TComponent> FlowTransient(FlowImplementationFactory<TComponent> createComponent)
        {
            return new FlowTransientComponentProvider<TComponent>(createComponent);
        }

        public IComponentProvider<TComponent> Bind<TDependency>(
            IComponentDescriptor<TDependency> dependencyDescriptor,
            DescriptorResolver<TComponent, TDependency> resolve)
            where TDependency : notnull
        {
            return dependencyDescriptor
                .Build(new Builder<TDependency>(configuration, dependencyDescriptor))
                .Bind(descriptor, resolve);
        }
    }
}