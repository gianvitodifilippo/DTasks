using DTasks.Infrastructure;

namespace DTasks.Configuration.DependencyInjection;

internal abstract class RootComponentProvider<TComponent> : IComponentProvider<TComponent>
    where TComponent : notnull
{
    protected abstract DTasksConfiguration Configuration { get; }
    
    protected abstract TComponent GetComponent();
    
    public TComponent GetComponent(IDAsyncScope scope) => GetComponent();

    public IComponentProvider<TResult> Bind<TResult>(IComponentDescriptor<TResult> resultDescriptor, DescriptorResolver<TResult, TComponent> resolveResult)
        where TResult : notnull
    {
        TComponent component = GetComponent();
        IComponentDescriptor<TResult> resolvedResultDescriptor = resolveResult(component);
        
        return ComponentProviderFactory.CreateProvider(Configuration, resolvedResultDescriptor);
    }
}