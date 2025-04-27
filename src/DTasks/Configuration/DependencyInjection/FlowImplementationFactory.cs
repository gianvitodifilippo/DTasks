using DTasks.Infrastructure;

namespace DTasks.Configuration.DependencyInjection;

public delegate TComponent FlowImplementationFactory<out TComponent>(IDAsyncFlow flow)
    where TComponent : notnull;
