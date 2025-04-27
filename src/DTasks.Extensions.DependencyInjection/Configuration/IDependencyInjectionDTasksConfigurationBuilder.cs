using DTasks.Extensions.DependencyInjection.Configuration;

namespace DTasks.Configuration;

public interface IDependencyInjectionDTasksConfigurationBuilder : IDependencyInjectionDTasksConfigurationBuilder<IDependencyInjectionDTasksConfigurationBuilder>;

public interface IDependencyInjectionDTasksConfigurationBuilder<out TBuilder> : IDTasksConfigurationBuilder<TBuilder>
    where TBuilder : IDependencyInjectionDTasksConfigurationBuilder<TBuilder>
{
    TBuilder ConfigureServices(Action<IServiceConfigurationBuilder> configure);
}
