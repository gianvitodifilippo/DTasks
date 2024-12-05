using DTasks.Marshaling;

namespace DTasks.Inspection.Dynamic;

internal interface IAwaiterManager
{
    TypeId GetTypeId(object awaiter);

    object CreateFromResult<TStateMachine>(TypeId awaiterId);
    
    object CreateFromResult<TStateMachine, TResult>(TypeId awaiterId, TResult result);
    
    object CreateFromException<TStateMachine>(TypeId awaiterId, Exception exception);
}
