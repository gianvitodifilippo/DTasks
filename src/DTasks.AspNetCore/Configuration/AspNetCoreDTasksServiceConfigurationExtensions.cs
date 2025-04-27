using DTasks.Serialization.Configuration;

namespace DTasks.Configuration;

public static class AspNetCoreDTasksServiceConfigurationExtensions
{
    public static IDependencyInjectionDTasksConfigurationBuilder UseAspNetCore(this IDependencyInjectionDTasksConfigurationBuilder builder)
    {
        return builder
            .AddAspNetCore()
            .UseSerialization(serialization => serialization
                .UseJsonFormat());
    }
}