using DTasks.Marshaling;

namespace DTasks.Serialization.Json;

internal interface IStateMachineSuspender<TStateMachine>
    where TStateMachine : notnull
{
    void Suspend(ref TStateMachine stateMachine, ISuspensionContext suspensionContext, ref readonly JsonStateMachineWriter writer);
}
