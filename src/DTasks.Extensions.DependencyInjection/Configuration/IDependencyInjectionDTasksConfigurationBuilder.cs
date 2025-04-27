using DTasks.Extensions.DependencyInjection.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Configuration;

public interface IDependencyInjectionDTasksConfigurationBuilder : IDependencyInjectionDTasksConfigurationBuilder<IDependencyInjectionDTasksConfigurationBuilder>;

public interface IDependencyInjectionDTasksConfigurationBuilder<out TBuilder> : IDTasksConfigurationBuilder<TBuilder>
    where TBuilder : IDependencyInjectionDTasksConfigurationBuilder<TBuilder>
{
    IServiceCollection Services { get; }

    TBuilder ConfigureServices(Action<IServiceConfigurationBuilder> configure);
}
