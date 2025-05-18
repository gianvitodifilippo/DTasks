using DTasks.Infrastructure;

namespace DTasks.Configuration;

public interface IDTasksConfigurationBuilder
{
    IDTasksConfigurationBuilder SetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, TProperty value);
    
    IDTasksConfigurationBuilder ConfigureMarshaling(Action<IMarshalingConfigurationBuilder> configure);

    IDTasksConfigurationBuilder ConfigureState(Action<IStateConfigurationBuilder> configure);

    IDTasksConfigurationBuilder ConfigureExecution(Action<IExecutionConfigurationBuilder> configure);
}
