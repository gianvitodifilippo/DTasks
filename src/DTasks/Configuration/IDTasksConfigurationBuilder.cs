namespace DTasks.Configuration;

public interface IDTasksConfigurationBuilder : IDTasksConfigurationBuilder<IDTasksConfigurationBuilder>;

public interface IDTasksConfigurationBuilder<out TBuilder>
    where TBuilder : IDTasksConfigurationBuilder<TBuilder>
{
    TBuilder ConfigureMarshaling(Action<IMarshalingConfigurationBuilder> configure);

    TBuilder ConfigureState(Action<IStateConfigurationBuilder> configure);

    TBuilder ConfigureExecution(Action<IExecutionConfigurationBuilder> configure);
}
