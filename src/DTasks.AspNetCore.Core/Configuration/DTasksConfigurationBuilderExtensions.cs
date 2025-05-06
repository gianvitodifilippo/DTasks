using DTasks.AspNetCore.Configuration;
using DTasks.AspNetCore.Execution;
using DTasks.AspNetCore.Infrastructure.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Configuration;

public static class DTasksConfigurationBuilderExtensions
{
    public static TBuilder AddAspNetCore<TBuilder>(this TBuilder builder)
        where TBuilder : IDependencyInjectionDTasksConfigurationBuilder
    {
        DTasksAspNetCoreCoreConfigurationBuilder aspNetCoreBuilder = new();
        
        return aspNetCoreBuilder.Configure(builder);
    }
    
    public static TBuilder AddAspNetCore<TBuilder>(this TBuilder builder, Action<IDTasksAspNetCoreCoreConfigurationBuilder> configure)
        where TBuilder : IDependencyInjectionDTasksConfigurationBuilder
    {
        DTasksAspNetCoreCoreConfigurationBuilder aspNetCoreBuilder = new();
        configure(aspNetCoreBuilder);
        
        return aspNetCoreBuilder.Configure(builder);
    }
}
