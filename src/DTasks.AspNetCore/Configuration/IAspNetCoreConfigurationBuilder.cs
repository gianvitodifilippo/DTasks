namespace DTasks.AspNetCore.Configuration;

public interface IAspNetCoreConfigurationBuilder
{
    IAspNetCoreConfigurationBuilder ConfigureSerialization(Action<IAspNetCoreSerializationConfigurationBuilder> configure);
}
