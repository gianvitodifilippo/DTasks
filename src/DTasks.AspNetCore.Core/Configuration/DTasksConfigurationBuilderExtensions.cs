using DTasks.AspNetCore.Infrastructure.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Configuration;

public static class DTasksConfigurationBuilderExtensions
{
    public static TBuilder AddAspNetCore<TBuilder>(this TBuilder builder)
        where TBuilder : IDependencyInjectionDTasksConfigurationBuilder
    {
        builder.Services
            .AddSingleton<IDAsyncContinuationFactory, DAsyncContinuationFactory>();

        builder
            .ConfigureMarshaling(marshaling => marshaling
                .RegisterTypeId(typeof(WebhookDAsyncContinuation.Surrogate))
                .RegisterTypeId(typeof(WebSocketsDAsyncContinuation.Surrogate)));

        return builder;
    }
}
