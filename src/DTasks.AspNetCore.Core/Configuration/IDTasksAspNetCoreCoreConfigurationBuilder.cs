using DTasks.AspNetCore.Execution;
using DTasks.Configuration;
using Microsoft.Extensions.Options;

namespace DTasks.AspNetCore.Configuration;

public interface IDTasksAspNetCoreCoreConfigurationBuilder
{
    IDependencyInjectionDTasksConfigurationBuilder DTasks { get; }

    IDTasksAspNetCoreCoreConfigurationBuilder RegisterEndpointResult<TResult>();
    
    IDTasksAspNetCoreCoreConfigurationBuilder AddResumptionEndpoint(ResumptionEndpoint endpoint);
    
    IDTasksAspNetCoreCoreConfigurationBuilder AddResumptionEndpoint<TResult>(ResumptionEndpoint<TResult> endpoint);
    
    IDTasksAspNetCoreCoreConfigurationBuilder AddDefaultResumptionEndpoint<TResult>();
    
    IDTasksAspNetCoreCoreConfigurationBuilder UseDTasksOptions(DTasksAspNetCoreOptions options);
    
    IDTasksAspNetCoreCoreConfigurationBuilder ConfigureDTasksOptions(Action<OptionsBuilder<DTasksAspNetCoreOptions>> configure);
}
