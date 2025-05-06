using DTasks.Configuration;
using DTasks.Extensions.DependencyInjection.Configuration;
using DTasks.Extensions.DependencyInjection.Infrastructure.Features;
using DTasks.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection.Infrastructure;

public abstract class ServicedDAsyncHost : DAsyncHost, IServiceProviderFeature
{
    private DTasksConfiguration? _configuration;

    protected abstract IServiceProvider Services { get; }
    
    IServiceProvider IServiceProviderFeature.Services => Services;

    protected sealed override DTasksConfiguration Configuration => _configuration ??= Services.GetRequiredService<DTasksConfiguration>();

    protected virtual void OnInitializeCore(IDAsyncFlowInitializationContext context)
    {
    }

    protected virtual void OnFinalizeCore(IDAsyncFlowFinalizationContext context)
    {
    }

    protected sealed override void OnInitialize(IDAsyncFlowInitializationContext context)
    {
        context.AddProperty(InfrastructureServiceProvider.ServiceProviderKey, Services);
        context.SetFeature<IServiceProviderFeature>(this);
        OnInitializeCore(context);
    }

    protected sealed override void OnFinalize(IDAsyncFlowFinalizationContext context)
    {
        _configuration = null;
        OnFinalizeCore(context);
    }
}