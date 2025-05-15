using DTasks.Configuration.DependencyInjection;

namespace DTasks.Infrastructure.DependencyInjection;

internal sealed class FlowComponentProvider(
    ComponentProvider parent,
    IDAsyncFlowScope scope) : ComponentProvider
{
    private bool _isActive;
    private readonly Dictionary<object, object?> _components = new();

    public override IDAsyncRootScope RootScope
    {
        get
        {
            CheckActive();

            return parent.RootScope;
        }
    }

    public override IDAsyncHostScope HostScope
    {
        get
        {
            CheckActive();

            return parent.HostScope;
        }
    }

    public override IDAsyncFlowScope FlowScope
    {
        get
        {
            CheckActive();
            
            return scope;
        }
    }

    public override TComponent GetRootComponent<TComponent>(IComponentToken<TComponent> token, Func<IComponentProvider, TComponent> createComponent, bool transient)
    {
        CheckActive();

        return parent.GetRootComponent(token, createComponent, transient);
    }

    public override TComponent GetHostComponent<TComponent>(IComponentToken<TComponent> token, Func<IComponentProvider, TComponent> createComponent, bool transient)
    {
        CheckActive();

        return parent.GetHostComponent(token, createComponent, transient);
    }

    public override TComponent GetFlowComponent<TComponent>(IComponentToken<TComponent> token, Func<IComponentProvider, TComponent> createComponent, bool transient)
    {
        CheckActive();
        
        if (_components.TryGetValue(token, out object? untypedComponent))
            return (TComponent)untypedComponent!;
        
        TComponent component = createComponent(this);
        if (!transient)
        {
            _components.TryAdd(token, component);
        }
        
        return component;
    }

    public void BeginScope()
    {
        _isActive = true;
    }

    public void EndScope()
    {
        _isActive = false;
        _components.Clear();
    }

    private void CheckActive()
    {
        if (!_isActive)
            throw new ObjectDisposedException(nameof(IDAsyncFlowScope), "The d-async flow terminated.");
    }
}