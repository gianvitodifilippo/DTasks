namespace DTasks.Configuration.DependencyInjection;

public delegate TComponent ConfiguredImplementationFactory<out TComponent>(DTasksConfiguration configuration)
    where TComponent : notnull;
