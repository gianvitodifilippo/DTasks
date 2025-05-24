using DTasks.AspNetCore.Configuration;
using DTasks.AspNetCore.Execution;
using DTasks.AspNetCore.Infrastructure.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Configuration;

public static class AspNetCoreCoreDTasksConfigurationBuilderExtensions
{
    public static TBuilder AddAspNetCore<TBuilder>(this TBuilder builder)
        where TBuilder : IDependencyInjectionDTasksConfigurationBuilder
    {
        DTasksAspNetCoreCoreConfigurationBuilder aspNetCoreBuilder = new(builder);
        
        return aspNetCoreBuilder.Configure(builder);
    }
    
    public static TBuilder AddAspNetCore<TBuilder>(this TBuilder builder, Action<IDTasksAspNetCoreCoreConfigurationBuilder> configure)
        where TBuilder : IDependencyInjectionDTasksConfigurationBuilder
    {
        DTasksAspNetCoreCoreConfigurationBuilder aspNetCoreBuilder = new(builder);
        configure(aspNetCoreBuilder);
        
        return aspNetCoreBuilder.Configure(builder);
    }
}
