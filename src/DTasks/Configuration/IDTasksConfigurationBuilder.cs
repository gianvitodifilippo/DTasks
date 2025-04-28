namespace DTasks.Configuration;

public interface IDTasksConfigurationBuilder
{
    IDTasksConfigurationBuilder ConfigureMarshaling(Action<IMarshalingConfigurationBuilder> configure);

    IDTasksConfigurationBuilder ConfigureState(Action<IStateConfigurationBuilder> configure);

    IDTasksConfigurationBuilder ConfigureExecution(Action<IExecutionConfigurationBuilder> configure);
}
