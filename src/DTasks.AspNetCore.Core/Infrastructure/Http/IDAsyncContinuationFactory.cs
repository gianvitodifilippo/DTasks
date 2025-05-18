using DTasks.AspNetCore.Http;
using DTasks.Infrastructure.Marshaling;
using Microsoft.AspNetCore.Http;

namespace DTasks.AspNetCore.Infrastructure.Http;

internal interface IDAsyncContinuationFactory
{
    bool TryCreateSurrogate(CallbackType callbackType, IHeaderDictionary headers, out TypedInstance<object> surrogate);
}