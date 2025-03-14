using System.Net.Http.Json;

namespace DTasks.AspNetCore;

public class WebhookDAsyncCallback(
    IHttpClientFactory httpClientFactory,
    Uri callbackAddress,
    string operationId) : IDAsyncCallback
{
    public async Task SucceedAsync(CancellationToken cancellationToken = default)
    {
        using HttpClient http = httpClientFactory.CreateClient();
        await http.PostAsJsonAsync(callbackAddress, new
        {
            operationId
        }, cancellationToken);
    }

    public async Task SucceedAsync<TResult>(TResult result, CancellationToken cancellationToken = default)
    {
        using HttpClient http = httpClientFactory.CreateClient();
        await http.PostAsJsonAsync(callbackAddress, new
        {
            operationId,
            result
        }, cancellationToken);
    }
}
