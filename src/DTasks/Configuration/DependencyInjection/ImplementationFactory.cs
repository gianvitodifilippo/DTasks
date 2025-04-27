namespace DTasks.Configuration.DependencyInjection;

public delegate TComponent ImplementationFactory<out TComponent>()
    where TComponent : notnull;
