namespace DTasks.Configuration.DependencyInjection;

public interface IComponentDescriptorVisitor<in TComponent>
{
    void VisitUnit(IComponentToken<TComponent> token);
    
    void VisitDescribe(Func<IComponentProvider, TComponent> createComponent, bool transient);

    void VisitBound<TDependency>(
        IComponentDescriptor<TDependency> dependencyDescriptor,
        ComponentDescriptorResolver<TComponent, TDependency> resolve);
}

public interface IComponentDescriptorVisitor<in TComponent, out TReturn>
{
    TReturn VisitUnit(IComponentToken<TComponent> token);
    
    TReturn VisitDescribe(Func<IComponentProvider, TComponent> createComponent, bool transient);

    TReturn VisitBound<TDependency>(
        IComponentDescriptor<TDependency> dependencyDescriptor,
        ComponentDescriptorResolver<TComponent, TDependency> resolve);
}