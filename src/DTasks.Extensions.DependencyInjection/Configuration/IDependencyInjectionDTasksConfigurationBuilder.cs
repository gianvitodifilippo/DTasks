using DTasks.Extensions.DependencyInjection.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Configuration;

public interface IDependencyInjectionDTasksConfigurationBuilder : IDTasksConfigurationBuilder
{
    IServiceCollection Services { get; }

    new IDependencyInjectionDTasksConfigurationBuilder ConfigureMarshaling(Action<IMarshalingConfigurationBuilder> configure);

    new IDependencyInjectionDTasksConfigurationBuilder ConfigureState(Action<IStateConfigurationBuilder> configure);

    new IDependencyInjectionDTasksConfigurationBuilder ConfigureExecution(Action<IExecutionConfigurationBuilder> configure);

    IDependencyInjectionDTasksConfigurationBuilder ConfigureServices(Action<IServiceConfigurationBuilder> configure);
}
