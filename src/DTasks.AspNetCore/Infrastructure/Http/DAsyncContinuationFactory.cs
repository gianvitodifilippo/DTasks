using DTasks.AspNetCore.Http;
using DTasks.Infrastructure.Marshaling;
using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore.Infrastructure.Http;

public sealed class DAsyncContinuationFactory : IDAsyncContinuationFactory
{
    // TODO: Add a builder interface to make configurable
    
    public bool TryCreateMemento(CallbackType callbackType, IHeaderDictionary headers, out TypedInstance<object> memento)
    {
        // TODO: We should validate the specific headers and return an error message if invalid
        
        if (callbackType == CallbackType.Webhook)
        {
            string callbackUrl = headers["Async-CallbackUrl"]!;
            memento = WebhookDAsyncContinuation.CreateMemento(new Uri(callbackUrl));
            return true;
        }

        if (callbackType == CallbackType.WebSockets)
        {
            string connectionId = headers["Async-ConnectionId"]!;
            memento = WebSocketsDAsyncContinuation.CreateMemento(connectionId);
            return true;
        }

        memento = default;
        return false;
    }
}