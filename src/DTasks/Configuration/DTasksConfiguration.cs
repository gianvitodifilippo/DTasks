using System.Collections.Immutable;
using DTasks.Configuration.DependencyInjection;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Configuration;

public sealed class DTasksConfiguration
{
    private DTasksConfiguration(DTasksConfigurationBuilder builder)
    {
        TypeResolver = builder.TypeResolver;
        SurrogatableTypes = builder.SurrogatableTypes;

        // BuildInfrastructure may use public properties of this instance,
        // therefore it must be call after those properties are assigned
        Infrastructure = builder.BuildInfrastructure(this);
    }

    public IDAsyncTypeResolver TypeResolver { get; }

    public ImmutableArray<Type> SurrogatableTypes { get; }

    internal IDAsyncInfrastructure Infrastructure { get; }

    internal IComponentProvider<TComponent> CreateProvider<TComponent>(IComponentDescriptor<TComponent> descriptor)
        where TComponent : notnull
    {
        return descriptor.Build(new ComponentProviderBuilder<TComponent>(descriptor, this));
    }

    public static DTasksConfiguration Create(Action<IDTasksConfigurationBuilder> configure)
    {
        DTasksConfigurationBuilder builder = new();
        configure(builder);

        return new(builder);
    }

    private sealed class ComponentProviderBuilder<TComponent>(
        IComponentDescriptor<TComponent> descriptor,
        DTasksConfiguration configuration) : IDAsyncInfrastructureBuilder<TComponent, IComponentProvider<TComponent>>
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
                .Build(new ComponentProviderBuilder<TDependency>(dependencyDescriptor, configuration))
                .Bind(descriptor, resolve);
        }
    }
}
