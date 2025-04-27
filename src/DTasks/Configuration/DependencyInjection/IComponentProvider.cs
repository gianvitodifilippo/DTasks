using DTasks.Infrastructure;

namespace DTasks.Configuration.DependencyInjection;

internal interface IComponentProvider<out TComponent>
    where TComponent : notnull
{
    TComponent GetComponent(IDAsyncScope scope);
    
    IComponentProvider<TResult> Bind<TResult>(IComponentDescriptor<TResult> resultDescriptor, DescriptorResolver<TResult, TComponent> resolveResult)
        where TResult : notnull;
}