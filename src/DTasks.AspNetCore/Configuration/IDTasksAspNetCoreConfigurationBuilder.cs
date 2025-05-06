namespace DTasks.AspNetCore.Configuration;

public interface IDTasksAspNetCoreConfigurationBuilder
{
    IDTasksAspNetCoreConfigurationBuilder ConfigureSerialization(Action<IAspNetCoreSerializationConfigurationBuilder> configure);
}
