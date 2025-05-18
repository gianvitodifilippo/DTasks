using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using DTasks.Configuration.DependencyInjection;
using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure.DependencyInjection;

internal sealed class HostComponentProvider(
    IDAsyncInfrastructure infrastructure,
    RootComponentProvider parent,
    IDAsyncHostScope scope) : ComponentProvider, IDAsyncHostInfrastructure
{
    private readonly ConcurrentDictionary<object, object?> _components = new();

    IDAsyncRootInfrastructure IDAsyncHostInfrastructure.Parent => parent;

    public override IDAsyncRootScope RootScope => parent.RootScope;

    public override IDAsyncHostScope HostScope => scope;

    public override IDAsyncFlowScope FlowScope => ScopeMismatch<IDAsyncFlowScope>();

    public FlowComponentProvider CreateFlowProvider(IDAsyncFlowScope flowScope)
    {
        return new FlowComponentProvider(this, flowScope);
    }

    public override TComponent GetRootComponent<TComponent>(IComponentToken<TComponent> token, Func<IComponentProvider, TComponent> createComponent, bool transient)
    {
        return parent.GetRootComponent(token, createComponent, transient);
    }

    public override TComponent GetHostComponent<TComponent>(IComponentToken<TComponent> token, Func<IComponentProvider, TComponent> createComponent, bool transient)
    {
        if (_components.TryGetValue(token, out object? untypedComponent))
            return (TComponent)untypedComponent!;
        
        TComponent component = createComponent(this);
        if (!transient)
        {
            _components.TryAdd(token, component);
        }
        
        return component;
    }

    public override TComponent GetFlowComponent<TComponent>(IComponentToken<TComponent> token, Func<IComponentProvider, TComponent> createComponent, bool transient)
    {
        return ScopeMismatch<TComponent>();
    }

    IDAsyncHeap IDAsyncHostInfrastructure.GetHeap()
    {
        return infrastructure.GetHeap(this);
    }

    public void Reset()
    {
        _components.Clear();
    }

    [DoesNotReturn]
    private static TComponent ScopeMismatch<TComponent>()
    {
        throw new InvalidOperationException($"Could not resolve requested component of type {typeof(TComponent).Name} from the host scope.");
    }
}