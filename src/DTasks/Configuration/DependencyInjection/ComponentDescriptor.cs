using System.ComponentModel;
using DTasks.Infrastructure;
using DTasks.Utils;

namespace DTasks.Configuration.DependencyInjection;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ComponentDescriptor
{
    public static IComponentDescriptor<TComponent> Singleton<TComponent>(TComponent component)
    {
        return Permanent(_ => component);
    }

    public static IComponentDescriptor<TComponent> Root<TComponent>(Func<IDAsyncRootScope, TComponent> createComponent)
    {
        return ComponentDescriptors.Root.Map(createComponent);
    }

    public static IComponentDescriptor<TComponent> RootTransient<TComponent>(Func<IDAsyncRootScope, TComponent> createComponent)
    {
        return ComponentDescriptors.Root.MapAsTransient(createComponent);
    }

    public static IComponentDescriptor<TComponent> Host<TComponent>(Func<IDAsyncHostScope, TComponent> createComponent)
    {
        return ComponentDescriptors.Host.Map(createComponent);
    }

    public static IComponentDescriptor<TComponent> HostTransient<TComponent>(Func<IDAsyncHostScope, TComponent> createComponent)
    {
        return ComponentDescriptors.Host.MapAsTransient(createComponent);
    }

    public static IComponentDescriptor<TComponent> Flow<TComponent>(Func<IDAsyncFlowScope, TComponent> createComponent)
    {
        return ComponentDescriptors.Flow.Map(createComponent);
    }

    public static IComponentDescriptor<TComponent> FlowTransient<TComponent>(Func<IDAsyncFlowScope, TComponent> createComponent)
    {
        return ComponentDescriptors.Flow.MapAsTransient(createComponent);
    }

    public static IComponentDescriptor<TResult> Map<TComponent, TResult>(
        this IComponentDescriptor<TComponent> descriptor,
        Func<TComponent, TResult> map)
    {
        ThrowHelper.ThrowIfNull(descriptor);
        ThrowHelper.ThrowIfNull(map);

        return descriptor
            .Bind(token => Permanent(provider => map(provider.GetComponent(token))));
    }
    
    public static IComponentDescriptor<TResult> MapAsTransient<TComponent, TResult>(
        this IComponentDescriptor<TComponent> descriptor,
        Func<TComponent, TResult> map)
    {
        ThrowHelper.ThrowIfNull(descriptor);
        ThrowHelper.ThrowIfNull(map);

        return descriptor
            .Bind(token => Transient(provider => map(provider.GetComponent(token))));
    }
    
    public static IComponentDescriptor<TResult> Combine<TComponent1, TComponent2, TResult>(
        IComponentDescriptor<TComponent1> descriptor1,
        IComponentDescriptor<TComponent2> descriptor2,
        Func<TComponent1, TComponent2, TResult> map)
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2);
        ThrowHelper.ThrowIfNull(map);

        return descriptor1
            .Bind(token1 => descriptor2
                .Bind(token2 => Permanent(provider => map(
                    provider.GetComponent(token1),
                    provider.GetComponent(token2)))));
    }
    
    public static IComponentDescriptor<TResult> CombineAsTransient<TComponent1, TComponent2, TResult>(
        IComponentDescriptor<TComponent1> descriptor1,
        IComponentDescriptor<TComponent2> descriptor2,
        Func<TComponent1, TComponent2, TResult> map)
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2);
        ThrowHelper.ThrowIfNull(map);

        return descriptor1
            .Bind(token1 => descriptor2
                .Bind(token2 => Transient(provider => map(
                    provider.GetComponent(token1),
                    provider.GetComponent(token2)))));
    }
    
    #region Primitive methods
    
    public static IComponentDescriptor<TComponent> Unit<TComponent>(IComponentToken<TComponent> token)
    {
        ThrowHelper.ThrowIfNull(token);
        
        return new UnitComponentDescriptor<TComponent>(token);
    }
    
    public static IComponentDescriptor<TComponent> Describe<TComponent>(Func<IComponentProvider, TComponent> createComponent, bool transient)
    {
        ThrowHelper.ThrowIfNull(createComponent);
        
        return new DescribeComponentDescriptor<TComponent>(createComponent, transient);
    }
    
    public static IComponentDescriptor<TComponent> Permanent<TComponent>(Func<IComponentProvider, TComponent> createComponent)
    {
        ThrowHelper.ThrowIfNull(createComponent);

        return new DescribeComponentDescriptor<TComponent>(createComponent, transient: false);
    }
    
    public static IComponentDescriptor<TComponent> Transient<TComponent>(Func<IComponentProvider, TComponent> createComponent)
    {
        ThrowHelper.ThrowIfNull(createComponent);

        return new DescribeComponentDescriptor<TComponent>(createComponent, transient: true);
    }
    
    public static IComponentDescriptor<TComponent> Bind<TComponent, TDependency>(
        this IComponentDescriptor<TDependency> dependencyDescriptor,
        ComponentDescriptorResolver<TComponent, TDependency> resolve)
    {
        ThrowHelper.ThrowIfNull(dependencyDescriptor);
        ThrowHelper.ThrowIfNull(resolve);

        return new BoundComponentDescriptor<TComponent, TDependency>(dependencyDescriptor, resolve);
    }

    #endregion

    #region LINQ methods
    
    public static IComponentDescriptor<TResult> Select<TComponent, TResult>(
        this IComponentDescriptor<TComponent> descriptor,
        Func<TComponent, TResult> selector)
    {
        ThrowHelper.ThrowIfNull(descriptor);
        ThrowHelper.ThrowIfNull(selector);

        return descriptor
            .Bind(token => Permanent(provider => selector(provider.GetComponent(token))));
    }
    
    public static IComponentDescriptor<TResult> SelectMany<TComponent1, TComponent2, TResult>(
        this IComponentDescriptor<TComponent1> descriptor1,
        Func<IComponentToken<TComponent1>, IComponentDescriptor<TComponent2>> descriptor2Selector,
        Func<TComponent1, TComponent2, TResult> selector)
    {
        ThrowHelper.ThrowIfNull(descriptor1);
        ThrowHelper.ThrowIfNull(descriptor2Selector);
        ThrowHelper.ThrowIfNull(selector);

        return descriptor1
            .Bind(token1 => descriptor2Selector(token1)
                .Bind(token2 => Permanent(provider => selector(
                    provider.GetComponent(token1),
                    provider.GetComponent(token2)))));
    }

    #endregion

    public static class Tokens
    {
        public static readonly IComponentToken<IDAsyncRootScope> Root = new Token<IDAsyncRootScope>();
        public static readonly IComponentToken<IDAsyncHostScope> Host = new Token<IDAsyncHostScope>();
        public static readonly IComponentToken<IDAsyncFlowScope> Flow = new Token<IDAsyncFlowScope>();
    }

    private sealed class Token<TComponent> : IComponentToken<TComponent>;
}