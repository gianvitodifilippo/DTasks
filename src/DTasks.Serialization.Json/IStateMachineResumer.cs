using DTasks.Infrastructure;

namespace DTasks.Serialization.Json;

internal interface IStateMachineResumer
{
    IDAsyncRunnable Resume(ref JsonStateMachineReader reader);

    IDAsyncRunnable Resume<TResult>(ref JsonStateMachineReader reader, TResult result);
}
