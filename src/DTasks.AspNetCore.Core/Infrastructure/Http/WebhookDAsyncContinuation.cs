using System.Net.Http.Json;
using DTasks.Infrastructure.Marshaling;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.AspNetCore.Infrastructure.Http;

internal sealed class WebhookDAsyncContinuation(IHttpClientFactory httpClientFactory, Uri callbackAddress) : IDAsyncContinuation
{
    public async Task OnSucceedAsync(DAsyncId flowId, CancellationToken cancellationToken = default)
    {
        using HttpClient http = httpClientFactory.CreateClient();
        await http.PostAsJsonAsync(callbackAddress, new
        {
            operationId = flowId.ToString()
        }, cancellationToken);
    }

    public async Task OnSucceedAsync<TResult>(DAsyncId flowId, TResult result, CancellationToken cancellationToken = default)
    {
        using HttpClient http = httpClientFactory.CreateClient();
        await http.PostAsJsonAsync(callbackAddress, new
        {
            operationId = flowId.ToString(),
            result
        }, cancellationToken);
    }

    public Task OnFailAsync(DAsyncId flowId, Exception exception, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task OnCancelAsync(DAsyncId flowId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public static TypedInstance<object> CreateSurrogate(Uri callbackAddress)
    {
        return new Surrogate(callbackAddress);
    }

    public sealed class Surrogate(Uri callbackAddress) : IDAsyncContinuationSurrogate
    {
        public Uri CallbackAddress { get; } = callbackAddress;
        
        public IDAsyncContinuation Restore(IServiceProvider services)
        {
            return new WebhookDAsyncContinuation(
                services.GetRequiredService<IHttpClientFactory>(),
                CallbackAddress);
        }
    }
}
