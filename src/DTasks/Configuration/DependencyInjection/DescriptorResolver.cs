namespace DTasks.Configuration.DependencyInjection;

public delegate IComponentDescriptor<TComponent> DescriptorResolver<out TComponent, in TDependency>(TDependency dependency)
    where TComponent : notnull;
