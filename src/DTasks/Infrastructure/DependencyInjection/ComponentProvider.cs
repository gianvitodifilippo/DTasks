using DTasks.Configuration.DependencyInjection;

namespace DTasks.Infrastructure.DependencyInjection;

internal abstract class ComponentProvider : IComponentProvider
{
    public abstract IDAsyncRootScope RootScope { get; }
    
    public abstract IDAsyncHostScope HostScope { get; }
    
    public abstract IDAsyncFlowScope FlowScope { get; }
    
    public abstract TComponent GetRootComponent<TComponent>(
        IComponentToken<TComponent> token,
        Func<IComponentProvider, TComponent> createComponent,
        bool transient);
    
    public abstract TComponent GetHostComponent<TComponent>(
        IComponentToken<TComponent> token,
        Func<IComponentProvider, TComponent> createComponent,
        bool transient);
    
    public abstract TComponent GetFlowComponent<TComponent>(
        IComponentToken<TComponent> token,
        Func<IComponentProvider, TComponent> createComponent,
        bool transient);
    
    public TComponent GetComponent<TComponent>(IComponentToken<TComponent> token)
    {
        return token is IInfrastructureComponentToken<TComponent> infrastructureToken
            ? infrastructureToken.CreateComponent(this)
            : GetWellKnownComponent(token);
    }
    
    private TComponent GetWellKnownComponent<TComponent>(IComponentToken<TComponent> token)
    {
        if (token == ComponentDescriptor.Tokens.Root)
            return (TComponent)RootScope;
        
        if (token == ComponentDescriptor.Tokens.Host)
            return (TComponent)HostScope;
        
        if (token == ComponentDescriptor.Tokens.Flow)
            return (TComponent)FlowScope;

        throw new ArgumentException($"Unrecognized component token of type '{token.GetType().Name}'.", nameof(token));
    }

}