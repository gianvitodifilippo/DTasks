using DTasks.AspNetCore.Execution;
using Microsoft.Extensions.Options;

namespace DTasks.AspNetCore.Configuration;

public interface IDTasksAspNetCoreCoreConfigurationBuilder
{
    IDTasksAspNetCoreCoreConfigurationBuilder AddResumptionEndpoint(ResumptionEndpoint endpoint);
    
    IDTasksAspNetCoreCoreConfigurationBuilder AddResumptionEndpoint<TResult>(ResumptionEndpoint<TResult> endpoint);
    
    IDTasksAspNetCoreCoreConfigurationBuilder AddDefaultResumptionEndpoint<TResult>();
    
    IDTasksAspNetCoreCoreConfigurationBuilder UseDTasksOptions(DTasksAspNetCoreOptions options);
    
    IDTasksAspNetCoreCoreConfigurationBuilder ConfigureDTasksOptions(Action<OptionsBuilder<DTasksAspNetCoreOptions>> configure);
}
