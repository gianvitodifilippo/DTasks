using System.ComponentModel;
using DTasks.Infrastructure;
using DTasks.Utils;

namespace DTasks.Configuration.DependencyInjection;

public static class ComponentDescriptor
{
    public static IComponentDescriptor<TComponent> Singleton<TComponent>(TComponent component)
        where TComponent : notnull
    {
        ThrowHelper.ThrowIfNull(component);

        return Unit(component);
    }
    
    public static IComponentDescriptor<TComponent> Singleton<TComponent>(ConfiguredImplementationFactory<TComponent> createComponent)
        where TComponent : notnull
    {
        ThrowHelper.ThrowIfNull(createComponent);

        return SingletonCore(createComponent);
    }
    
    public static IComponentDescriptor<TComponent> Scoped<TComponent>(FlowImplementationFactory<TComponent> createComponent)
        where TComponent : notnull
    {
        ThrowHelper.ThrowIfNull(createComponent);

        return ScopedCore(createComponent);
    }
    
    public static IComponentDescriptor<TComponent> Transient<TComponent>(ImplementationFactory<TComponent> createComponent)
        where TComponent : notnull
    {
        ThrowHelper.ThrowIfNull(createComponent);

        return RootTransient(createComponent);
    }
    
    public static IComponentDescriptor<TComponent> Transient<TComponent>(ConfiguredImplementationFactory<TComponent> createComponent)
        where TComponent : notnull
    {
        ThrowHelper.ThrowIfNull(createComponent);

        return RootTransient(createComponent);
    }
    
    public static IComponentDescriptor<TComponent> Transient<TComponent>(FlowImplementationFactory<TComponent> createComponent)
        where TComponent : notnull
    {
        ThrowHelper.ThrowIfNull(createComponent);

        return FlowTransient(createComponent);
    }

    public static IComponentDescriptor<TResult> Aggregate<TComponent, TResult>(
        IEnumerable<IComponentDescriptor<TComponent>> descriptors,
        Func<IEnumerable<TComponent>, TResult> aggregate)
        where TComponent : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptors);
        ThrowHelper.ThrowIfNull(aggregate);

        IComponentDescriptor<List<TComponent>> seed = Unit<List<TComponent>>([]);
        return descriptors
            .Aggregate(seed, (accumulated, descriptor) => accumulated
                .Bind(list => descriptor
                    .Map<TComponent, List<TComponent>>(component => [..list, component])))
            .Map(aggregate);
    }

    #region Map/MapAsTransient/FlatMap

    public static IComponentDescriptor<TResult> Map<TComponent, TResult>(
        this IComponentDescriptor<TComponent> descriptor,
        Func<TComponent, TResult> map)
        where TComponent : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor);
        ThrowHelper.ThrowIfNull(map);
        
        return descriptor.Bind(dependency => Unit(map(dependency)));
    }
    
    public static IComponentDescriptor<TResult> Map<TComponent, TResult>(
        this IComponentDescriptor<TComponent> descriptor,
        Func<DTasksConfiguration, TComponent, TResult> map)
        where TComponent : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor);
        ThrowHelper.ThrowIfNull(map);
        
        return descriptor.Bind(dependency => SingletonCore(config => map(config, dependency)));
    }

    public static IComponentDescriptor<TResult> Map<TComponent, TResult>(
        this IComponentDescriptor<TComponent> descriptor,
        Func<IDAsyncFlow, TComponent, TResult> map)
        where TComponent : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor);
        ThrowHelper.ThrowIfNull(map);
        
        return descriptor.Bind(dependency => ScopedCore(flow => map(flow, dependency)));
    }

    public static IComponentDescriptor<TResult> MapAsTransient<TComponent, TResult>(
        this IComponentDescriptor<TComponent> descriptor,
        Func<TComponent, TResult> map)
        where TComponent : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor);
        ThrowHelper.ThrowIfNull(map);
        
        return descriptor.Bind(dependency => RootTransient(() => map(dependency)));
    }

    public static IComponentDescriptor<TResult> MapAsTransient<TComponent, TResult>(
        this IComponentDescriptor<TComponent> descriptor,
        Func<DTasksConfiguration, TComponent, TResult> map)
        where TComponent : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor);
        ThrowHelper.ThrowIfNull(map);
        
        return descriptor.Bind(dependency => RootTransient(config => map(config, dependency)));
    }

    public static IComponentDescriptor<TResult> MapAsTransient<TComponent, TResult>(
        this IComponentDescriptor<TComponent> descriptor,
        Func<IDAsyncFlow, TComponent, TResult> map)
        where TComponent : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor);
        ThrowHelper.ThrowIfNull(map);
        
        return descriptor.Bind(dependency => FlowTransient(flow => map(flow, dependency)));
    }

    public static IComponentDescriptor<TResult> FlatMap<TComponent, TResult>(
        this IComponentDescriptor<TComponent> descriptor,
        DescriptorResolver<TResult, TComponent> resolve)
        where TComponent : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor);
        ThrowHelper.ThrowIfNull(resolve);

        return descriptor.Bind(resolve);
    }

    #endregion

    #region Combine/CombineAsTransient

    public static IComponentDescriptor<TResult> Combine<TComponent1, TComponent2, TResult>(
        IComponentDescriptor<TComponent1> descriptor1,
        IComponentDescriptor<TComponent2> descriptor2,
        Func<TComponent1, TComponent2, TResult> combine)
        where TComponent1 : notnull
        where TComponent2 : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2);
        ThrowHelper.ThrowIfNull(combine);

        return descriptor1
            .Bind(component1 => descriptor2
                .Bind(component2 => Unit(
                    combine(component1, component2))));
    }

    public static IComponentDescriptor<TResult> Combine<TComponent1, TComponent2, TResult>(
        IComponentDescriptor<TComponent1> descriptor1,
        IComponentDescriptor<TComponent2> descriptor2,
        Func<DTasksConfiguration, TComponent1, TComponent2, TResult> combine)
        where TComponent1 : notnull
        where TComponent2 : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2);
        ThrowHelper.ThrowIfNull(combine);

        return descriptor1
            .Bind(component1 => descriptor2
                .Bind(component2 => SingletonCore(
                    config => combine(config, component1, component2))));
    }

    public static IComponentDescriptor<TResult> Combine<TComponent1, TComponent2, TResult>(
        IComponentDescriptor<TComponent1> descriptor1,
        IComponentDescriptor<TComponent2> descriptor2,
        Func<IDAsyncFlow, TComponent1, TComponent2, TResult> combine)
        where TComponent1 : notnull
        where TComponent2 : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2);
        ThrowHelper.ThrowIfNull(combine);

        return descriptor1
            .Bind(component1 => descriptor2
                .Bind(component2 => ScopedCore(
                    flow => combine(flow, component1, component2))));
    }

    public static IComponentDescriptor<TResult> CombineAsTransient<TComponent1, TComponent2, TResult>(
        IComponentDescriptor<TComponent1> descriptor1,
        IComponentDescriptor<TComponent2> descriptor2,
        Func<TComponent1, TComponent2, TResult> combine)
        where TComponent1 : notnull
        where TComponent2 : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2);
        ThrowHelper.ThrowIfNull(combine);

        return descriptor1
            .Bind(component1 => descriptor2
                .Bind(component2 => RootTransient(
                    () => combine(component1, component2))));
    }

    public static IComponentDescriptor<TResult> CombineAsTransient<TComponent1, TComponent2, TResult>(
        IComponentDescriptor<TComponent1> descriptor1,
        IComponentDescriptor<TComponent2> descriptor2,
        Func<DTasksConfiguration, TComponent1, TComponent2, TResult> combine)
        where TComponent1 : notnull
        where TComponent2 : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2);
        ThrowHelper.ThrowIfNull(combine);

        return descriptor1
            .Bind(component1 => descriptor2
                .Bind(component2 => RootTransient(
                    config => combine(config, component1, component2))));
    }

    public static IComponentDescriptor<TResult> CombineAsTransient<TComponent1, TComponent2, TResult>(
        IComponentDescriptor<TComponent1> descriptor1,
        IComponentDescriptor<TComponent2> descriptor2,
        Func<IDAsyncFlow, TComponent1, TComponent2, TResult> combine)
        where TComponent1 : notnull
        where TComponent2 : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2);
        ThrowHelper.ThrowIfNull(combine);

        return descriptor1
            .Bind(component1 => descriptor2
                .Bind(component2 => FlowTransient(
                    flow => combine(flow, component1, component2))));
    }

    public static IComponentDescriptor<TResult> Combine<TComponent1, TComponent2, TComponent3, TResult>(
        IComponentDescriptor<TComponent1> descriptor1,
        IComponentDescriptor<TComponent2> descriptor2,
        IComponentDescriptor<TComponent3> descriptor3,
        Func<TComponent1, TComponent2, TComponent3, TResult> combine)
        where TComponent1 : notnull
        where TComponent2 : notnull
        where TComponent3 : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2);
        ThrowHelper.ThrowIfNull(descriptor3);
        ThrowHelper.ThrowIfNull(combine);

        return descriptor1
            .Bind(component1 => descriptor2
                .Bind(component2 => descriptor3
                    .Bind(component3 => Unit(
                        combine(component1, component2, component3)))));
    }

    public static IComponentDescriptor<TResult> Combine<TComponent1, TComponent2, TComponent3, TResult>(
        IComponentDescriptor<TComponent1> descriptor1,
        IComponentDescriptor<TComponent2> descriptor2,
        IComponentDescriptor<TComponent3> descriptor3,
        Func<DTasksConfiguration, TComponent1, TComponent2, TComponent3, TResult> combine)
        where TComponent1 : notnull
        where TComponent2 : notnull
        where TComponent3 : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2);
        ThrowHelper.ThrowIfNull(descriptor3);
        ThrowHelper.ThrowIfNull(combine);

        return descriptor1
            .Bind(component1 => descriptor2
                .Bind(component2 => descriptor3
                    .Bind(component3 => SingletonCore(
                        config => combine(config, component1, component2, component3)))));
    }

    public static IComponentDescriptor<TResult> Combine<TComponent1, TComponent2, TComponent3, TResult>(
        IComponentDescriptor<TComponent1> descriptor1,
        IComponentDescriptor<TComponent2> descriptor2,
        IComponentDescriptor<TComponent3> descriptor3,
        Func<IDAsyncFlow, TComponent1, TComponent2, TComponent3, TResult> combine)
        where TComponent1 : notnull
        where TComponent2 : notnull
        where TComponent3 : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2);
        ThrowHelper.ThrowIfNull(descriptor3);
        ThrowHelper.ThrowIfNull(combine);

        return descriptor1
            .Bind(component1 => descriptor2
                .Bind(component2 => descriptor3
                    .Bind(component3 => ScopedCore(
                        flow => combine(flow, component1, component2, component3)))));
    }

    public static IComponentDescriptor<TResult> CombineAsTransient<TComponent1, TComponent2, TComponent3, TResult>(
        IComponentDescriptor<TComponent1> descriptor1,
        IComponentDescriptor<TComponent2> descriptor2,
        IComponentDescriptor<TComponent3> descriptor3,
        Func<TComponent1, TComponent2, TComponent3, TResult> combine)
        where TComponent1 : notnull
        where TComponent2 : notnull
        where TComponent3 : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2);
        ThrowHelper.ThrowIfNull(descriptor3);
        ThrowHelper.ThrowIfNull(combine);

        return descriptor1
            .Bind(component1 => descriptor2
                .Bind(component2 => descriptor3
                    .Bind(component3 => RootTransient(
                        () => combine(component1, component2, component3)))));
    }

    public static IComponentDescriptor<TResult> CombineAsTransient<TComponent1, TComponent2, TComponent3, TResult>(
        IComponentDescriptor<TComponent1> descriptor1,
        IComponentDescriptor<TComponent2> descriptor2,
        IComponentDescriptor<TComponent3> descriptor3,
        Func<DTasksConfiguration, TComponent1, TComponent2, TComponent3, TResult> combine)
        where TComponent1 : notnull
        where TComponent2 : notnull
        where TComponent3 : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2);
        ThrowHelper.ThrowIfNull(descriptor3);
        ThrowHelper.ThrowIfNull(combine);

        return descriptor1
            .Bind(component1 => descriptor2
                .Bind(component2 => descriptor3
                    .Bind(component3 => RootTransient(
                        config => combine(config, component1, component2, component3)))));
    }

    public static IComponentDescriptor<TResult> CombineAsTransient<TComponent1, TComponent2, TComponent3, TResult>(
        IComponentDescriptor<TComponent1> descriptor1,
        IComponentDescriptor<TComponent2> descriptor2,
        IComponentDescriptor<TComponent3> descriptor3,
        Func<IDAsyncFlow, TComponent1, TComponent2, TComponent3, TResult> combine)
        where TComponent1 : notnull
        where TComponent2 : notnull
        where TComponent3 : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2);
        ThrowHelper.ThrowIfNull(descriptor3);
        ThrowHelper.ThrowIfNull(combine);

        return descriptor1
            .Bind(component1 => descriptor2
                .Bind(component2 => descriptor3
                    .Bind(component3 => FlowTransient(flow => combine(flow, component1, component2, component3)))));
    }

    public static IComponentDescriptor<TResult> Combine<TComponent1, TComponent2, TComponent3, TComponent4, TResult>(
        IComponentDescriptor<TComponent1> descriptor1,
        IComponentDescriptor<TComponent2> descriptor2,
        IComponentDescriptor<TComponent3> descriptor3,
        IComponentDescriptor<TComponent4> descriptor4,
        Func<TComponent1, TComponent2, TComponent3, TComponent4, TResult> combine)
        where TComponent1 : notnull
        where TComponent2 : notnull
        where TComponent3 : notnull
        where TComponent4 : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2);
        ThrowHelper.ThrowIfNull(descriptor3);
        ThrowHelper.ThrowIfNull(descriptor4);
        ThrowHelper.ThrowIfNull(combine);

        return descriptor1
            .Bind(component1 => descriptor2
                .Bind(component2 => descriptor3
                    .Bind(component3 => descriptor4
                        .Bind(component4 => Unit(
                            combine(component1, component2, component3, component4))))));
    }

    public static IComponentDescriptor<TResult> Combine<TComponent1, TComponent2, TComponent3, TComponent4, TResult>(
        IComponentDescriptor<TComponent1> descriptor1,
        IComponentDescriptor<TComponent2> descriptor2,
        IComponentDescriptor<TComponent3> descriptor3,
        IComponentDescriptor<TComponent4> descriptor4,
        Func<DTasksConfiguration, TComponent1, TComponent2, TComponent3, TComponent4, TResult> combine)
        where TComponent1 : notnull
        where TComponent2 : notnull
        where TComponent3 : notnull
        where TComponent4 : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2);
        ThrowHelper.ThrowIfNull(descriptor3);
        ThrowHelper.ThrowIfNull(descriptor4);
        ThrowHelper.ThrowIfNull(combine);

        return descriptor1
            .Bind(component1 => descriptor2
                .Bind(component2 => descriptor3
                    .Bind(component3 => descriptor4
                        .Bind(component4 => SingletonCore(
                            config => combine(config, component1, component2, component3, component4))))));
    }

    public static IComponentDescriptor<TResult> Combine<TComponent1, TComponent2, TComponent3, TComponent4, TResult>(
        IComponentDescriptor<TComponent1> descriptor1,
        IComponentDescriptor<TComponent2> descriptor2,
        IComponentDescriptor<TComponent3> descriptor3,
        IComponentDescriptor<TComponent4> descriptor4,
        Func<IDAsyncFlow, TComponent1, TComponent2, TComponent3, TComponent4, TResult> combine)
        where TComponent1 : notnull
        where TComponent2 : notnull
        where TComponent3 : notnull
        where TComponent4 : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2);
        ThrowHelper.ThrowIfNull(descriptor3);
        ThrowHelper.ThrowIfNull(descriptor4);
        ThrowHelper.ThrowIfNull(combine);

        return descriptor1
            .Bind(component1 => descriptor2
                .Bind(component2 => descriptor3
                    .Bind(component3 => descriptor4
                        .Bind(component4 => ScopedCore(
                            flow => combine(flow, component1, component2, component3, component4))))));
    }

    public static IComponentDescriptor<TResult> CombineAsTransient<TComponent1, TComponent2, TComponent3, TComponent4, TResult>(
        IComponentDescriptor<TComponent1> descriptor1,
        IComponentDescriptor<TComponent2> descriptor2,
        IComponentDescriptor<TComponent3> descriptor3,
        IComponentDescriptor<TComponent4> descriptor4,
        Func<TComponent1, TComponent2, TComponent3, TComponent4, TResult> combine)
        where TComponent1 : notnull
        where TComponent2 : notnull
        where TComponent3 : notnull
        where TComponent4 : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2);
        ThrowHelper.ThrowIfNull(descriptor3);
        ThrowHelper.ThrowIfNull(descriptor4);
        ThrowHelper.ThrowIfNull(combine);

        return descriptor1
            .Bind(component1 => descriptor2
                .Bind(component2 => descriptor3
                    .Bind(component3 => descriptor4
                        .Bind(component4 => RootTransient(
                            () => combine(component1, component2, component3, component4))))));
    }

    public static IComponentDescriptor<TResult> CombineAsTransient<TComponent1, TComponent2, TComponent3, TComponent4, TResult>(
        IComponentDescriptor<TComponent1> descriptor1,
        IComponentDescriptor<TComponent2> descriptor2,
        IComponentDescriptor<TComponent3> descriptor3,
        IComponentDescriptor<TComponent4> descriptor4,
        Func<DTasksConfiguration, TComponent1, TComponent2, TComponent3, TComponent4, TResult> combine)
        where TComponent1 : notnull
        where TComponent2 : notnull
        where TComponent3 : notnull
        where TComponent4 : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2);
        ThrowHelper.ThrowIfNull(descriptor3);
        ThrowHelper.ThrowIfNull(descriptor4);
        ThrowHelper.ThrowIfNull(combine);

        return descriptor1
            .Bind(component1 => descriptor2
                .Bind(component2 => descriptor3
                    .Bind(component3 => descriptor4
                        .Bind(component4 => RootTransient(
                            config => combine(config, component1, component2, component3, component4))))));
    }

    public static IComponentDescriptor<TResult> CombineAsTransient<TComponent1, TComponent2, TComponent3, TComponent4, TResult>(
        IComponentDescriptor<TComponent1> descriptor1,
        IComponentDescriptor<TComponent2> descriptor2,
        IComponentDescriptor<TComponent3> descriptor3,
        IComponentDescriptor<TComponent4> descriptor4,
        Func<IDAsyncFlow, TComponent1, TComponent2, TComponent3, TComponent4, TResult> combine)
        where TComponent1 : notnull
        where TComponent2 : notnull
        where TComponent3 : notnull
        where TComponent4 : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2);
        ThrowHelper.ThrowIfNull(descriptor3);
        ThrowHelper.ThrowIfNull(descriptor4);
        ThrowHelper.ThrowIfNull(combine);

        return descriptor1
            .Bind(component1 => descriptor2
                .Bind(component2 => descriptor3
                    .Bind(component3 => descriptor4
                        .Bind(component4 => FlowTransient(
                            flow => combine(flow, component1, component2, component3, component4))))));
    }

    #endregion

    #region LINQ methods

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IComponentDescriptor<TResult> Select<TComponent, TResult>(
        this IComponentDescriptor<TComponent> descriptor,
        Func<TComponent, TResult> selector)
        where TComponent : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor);
        ThrowHelper.ThrowIfNull(selector);

        return descriptor.Bind(component => Unit(selector(component)));
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IComponentDescriptor<TResult> SelectMany<TComponent1, TComponent2, TResult>(
        this IComponentDescriptor<TComponent1> descriptor1,
        Func<TComponent1, IComponentDescriptor<TComponent2>> descriptor2Selector,
        Func<TComponent1, TComponent2, TResult> resultSelector)
        where TComponent1 : notnull
        where TComponent2 : notnull
        where TResult : notnull
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2Selector);
        ThrowHelper.ThrowIfNull(resultSelector);

        return descriptor1
            .Bind(component1 => descriptor2Selector(component1)
                .Bind(component2 => Unit(resultSelector(component1, component2))));
    }

    #endregion
    
    private static UnitComponentDescriptor<TComponent> Unit<TComponent>(TComponent component)
        where TComponent : notnull
    {
        return new UnitComponentDescriptor<TComponent>(component);
    }
    
    private static IComponentDescriptor<TComponent> SingletonCore<TComponent>(ConfiguredImplementationFactory<TComponent> createComponent)
        where TComponent : notnull
    {
        return new SingletonComponentDescriptor<TComponent>(createComponent);
    }
    
    private static IComponentDescriptor<TComponent> ScopedCore<TComponent>(FlowImplementationFactory<TComponent> createComponent)
        where TComponent : notnull
    {
        return new ScopedComponentDescriptor<TComponent>(createComponent);
    }
    
    private static IComponentDescriptor<TComponent> RootTransient<TComponent>(ConfiguredImplementationFactory<TComponent> createComponent)
        where TComponent : notnull
    {
        return new RootTransientComponentDescriptor<TComponent>(createComponent);
    }
    
    private static IComponentDescriptor<TComponent> RootTransient<TComponent>(ImplementationFactory<TComponent> createComponent)
        where TComponent : notnull
    {
        return new RootTransientComponentDescriptor<TComponent>(config => createComponent());
    }
    
    private static IComponentDescriptor<TComponent> FlowTransient<TComponent>(FlowImplementationFactory<TComponent> createComponent)
        where TComponent : notnull
    {
        return new FlowTransientComponentDescriptor<TComponent>(createComponent);
    }

    private static BoundComponentDescriptor<TComponent, TDependency> Bind<TComponent, TDependency>(
        this IComponentDescriptor<TDependency> dependencyDescriptor,
        DescriptorResolver<TComponent, TDependency> resolve)
        where TComponent : notnull
        where TDependency : notnull
    {
        return new BoundComponentDescriptor<TComponent, TDependency>(dependencyDescriptor, resolve);
    }
}