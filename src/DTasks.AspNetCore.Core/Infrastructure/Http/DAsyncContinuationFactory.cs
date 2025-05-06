using DTasks.AspNetCore.Http;
using DTasks.Infrastructure.Marshaling;
using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore.Infrastructure.Http;

internal sealed class DAsyncContinuationFactory : IDAsyncContinuationFactory
{
    // TODO: Add a builder interface to make configurable

    public bool TryCreateSurrogate(CallbackType callbackType, IHeaderDictionary headers, out TypedInstance<object> surrogate)
    {
        // TODO: We should validate the specific headers and return an error message if invalid

        if (callbackType == CallbackType.Webhook)
        {
            string callbackUrl = headers["Async-CallbackUrl"]!;
            surrogate = WebhookDAsyncContinuation.CreateSurrogate(new Uri(callbackUrl));
            return true;
        }

        if (callbackType == CallbackType.WebSockets)
        {
            string connectionId = headers["Async-ConnectionId"]!;
            surrogate = WebSocketsDAsyncContinuation.CreateSurrogate(connectionId);
            return true;
        }

        surrogate = default;
        return false;
    }
}