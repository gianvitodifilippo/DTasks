using DTasks.AspNetCore.Infrastructure.Http;

namespace DTasks.Configuration;

public static class DTasksConfigurationBuilderExtensions
{
    public static TBuilder AddAspNetCore<TBuilder>(this TBuilder builder)
        where TBuilder : IDTasksConfigurationBuilder<TBuilder>
    {
        return builder
            .ConfigureMarshaling(marshaling => marshaling
                .RegisterTypeId(typeof(WebhookDAsyncContinuation.Surrogate))
                .RegisterTypeId(typeof(WebSocketsDAsyncContinuation.Surrogate)));
    }
}