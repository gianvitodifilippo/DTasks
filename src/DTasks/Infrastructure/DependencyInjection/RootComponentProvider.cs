using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using DTasks.Configuration.DependencyInjection;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Infrastructure.DependencyInjection;

internal sealed class RootComponentProvider(IDAsyncInfrastructure infrastructure) : ComponentProvider, IDAsyncRootInfrastructure
{
    private readonly ConcurrentDictionary<object, object?> _components = new();
    
    public override IDAsyncRootScope RootScope => infrastructure.RootScope;

    public override IDAsyncHostScope HostScope => ScopeMismatch<IDAsyncHostScope>();

    public override IDAsyncFlowScope FlowScope => ScopeMismatch<IDAsyncFlowScope>();

    IDAsyncTypeResolver IDAsyncRootInfrastructure.TypeResolver => infrastructure.TypeResolver;

    public HostComponentProvider CreateHostProvider(IDAsyncHostScope hostScope)
    {
        return new HostComponentProvider(infrastructure, this, hostScope);
    }

    public override TComponent GetRootComponent<TComponent>(
        IComponentToken<TComponent> token,
        Func<IComponentProvider, TComponent> createComponent,
        bool transient)
    {
        if (_components.TryGetValue(token, out object? untypedComponent))
            return (TComponent)untypedComponent!;
        
        TComponent component = createComponent(this);
        if (transient)
        {
            _components.TryAdd(token, component);
        }
        
        return component;
    }

    public override TComponent GetHostComponent<TComponent>(
        IComponentToken<TComponent> token,
        Func<IComponentProvider, TComponent> createComponent,
        bool transient)
    {
        return ScopeMismatch<TComponent>();
    }

    public override TComponent GetFlowComponent<TComponent>(
        IComponentToken<TComponent> token,
        Func<IComponentProvider, TComponent> createComponent,
        bool transient)
    {
        return ScopeMismatch<TComponent>();
    }

    [DoesNotReturn]
    private static TComponent ScopeMismatch<TComponent>()
    {
        throw new InvalidOperationException($"Could not resolve requested component of type {typeof(TComponent).Name} from the root scope.");
    }
}