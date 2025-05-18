using DTasks.AspNetCore.Execution;
using DTasks.Configuration;
using Microsoft.Extensions.Options;

namespace DTasks.AspNetCore.Configuration;

public interface IDTasksAspNetCoreConfigurationBuilder : IDTasksAspNetCoreCoreConfigurationBuilder
{
    new IDependencyInjectionDTasksConfigurationBuilder DTasks { get; }
    
    new IDTasksAspNetCoreConfigurationBuilder AddResumptionEndpoint(ResumptionEndpoint endpoint);
    
    new IDTasksAspNetCoreConfigurationBuilder AddResumptionEndpoint<TResult>(ResumptionEndpoint<TResult> endpoint);
    
    new IDTasksAspNetCoreConfigurationBuilder AddDefaultResumptionEndpoint<TResult>();
    
    new IDTasksAspNetCoreConfigurationBuilder UseDTasksOptions(DTasksAspNetCoreOptions options);
    
    new IDTasksAspNetCoreConfigurationBuilder ConfigureDTasksOptions(Action<OptionsBuilder<DTasksAspNetCoreOptions>> configure);
    
    IDTasksAspNetCoreConfigurationBuilder ConfigureSerialization(Action<IAspNetCoreSerializationConfigurationBuilder> configure);
}
