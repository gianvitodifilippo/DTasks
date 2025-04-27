using DTasks.Configuration;
using DTasks.Extensions.DependencyInjection.Configuration;
using DTasks.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection.Infrastructure;

public abstract class ServicedDAsyncHost : DAsyncHost
{
    private DTasksConfiguration? _configuration;
    
    protected abstract IServiceProvider Services { get; }
    
    protected sealed override DTasksConfiguration Configuration => _configuration ??= Services.GetRequiredService<DTasksConfiguration>();

    protected virtual void OnInitializeCore(IDAsyncFlowInitializationContext context)
    {
    }

    protected virtual void OnFinalizeCore(IDAsyncFlowFinalizationContext context)
    {
    }

    protected override void OnInitialize(IDAsyncFlowInitializationContext context)
    {
        context.AddProperty(InfrastructureServiceProvider.ServiceProviderKey, Services);
        OnInitializeCore(context);
    }

    protected override void OnFinalize(IDAsyncFlowFinalizationContext context)
    {
        _configuration = null;
        context.RemoveProperty(InfrastructureServiceProvider.ServiceProviderKey);
        OnFinalizeCore(context);
    }
}