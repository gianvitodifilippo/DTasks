using DTasks.Infrastructure.Marshaling;
using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore.Infrastructure.Http;

public interface IDAsyncCallbackFactory
{
    bool TryCreateMemento(string callbackType, IHeaderDictionary headers, out TypedInstance<object> memento);
}