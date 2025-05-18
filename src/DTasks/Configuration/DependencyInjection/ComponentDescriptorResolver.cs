namespace DTasks.Configuration.DependencyInjection;

public delegate IComponentDescriptor<TComponent> ComponentDescriptorResolver<out TComponent, in TDependency>(IComponentToken<TDependency> dependencyToken);
