using System.Diagnostics.CodeAnalysis;
using DTasks.Infrastructure.Marshaling;
using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore;

public interface IDAsyncCallbackFactory
{
    bool TryCreateMemento(HttpContext context, out TypedInstance<object> memento);
}