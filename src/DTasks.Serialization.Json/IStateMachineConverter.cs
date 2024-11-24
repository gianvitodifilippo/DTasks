using DTasks.Hosting;
using DTasks.Marshaling;

namespace DTasks.Serialization.Json;

internal interface IStateMachineConverter<TStateMachine>
    where TStateMachine : notnull
{
    void Suspend(ref TStateMachine stateMachine, ISuspensionContext suspensionContext, ref readonly JsonStateMachineWriter writer);

    IDAsyncRunnable Resume(ref JsonStateMachineReader reader);

    IDAsyncRunnable Resume<TResult>(ref JsonStateMachineReader reader, TResult result);
}
