namespace DTasks.Configuration.DependencyInjection;

internal sealed class BoundComponentDescriptor<TComponent, TDependency>(
    IComponentDescriptor<TDependency> dependencyDescriptor,
    ComponentDescriptorResolver<TComponent, TDependency> resolve) : IComponentDescriptor<TComponent>
{
    public void Accept(IComponentDescriptorVisitor<TComponent> visitor)
    {
        visitor.VisitBound(dependencyDescriptor, resolve);
    }

    public TReturn Accept<TReturn>(IComponentDescriptorVisitor<TComponent, TReturn> visitor)
    {
        return visitor.VisitBound(dependencyDescriptor, resolve);
    }
}