using DTasks.Marshaling;

namespace DTasks.Inspection.Dynamic;

internal interface IAwaiterManager
{
    TypeId GetTypeId(object awaiter);

    object CreateFromResult(TypeId awaiterId);
    
    object CreateFromResult<TResult>(TypeId awaiterId, TResult result);
    
    object CreateFromException(TypeId awaiterId, Exception exception);
}
