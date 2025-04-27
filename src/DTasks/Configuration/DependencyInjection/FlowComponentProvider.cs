using DTasks.Infrastructure;

namespace DTasks.Configuration.DependencyInjection;

internal abstract class FlowComponentProvider<TComponent> : IComponentProvider<TComponent>
    where TComponent : notnull
{
    public abstract TComponent GetComponent(IDAsyncScope scope);

    public IComponentProvider<TResult> Bind<TResult>(
        IComponentDescriptor<TResult> resultDescriptor,
        DescriptorResolver<TResult, TComponent> resolveResult)
        where TResult : notnull
    {
        return new BoundComponentProvider<TResult, TComponent>(this, resultDescriptor, resolveResult);
    }
}
