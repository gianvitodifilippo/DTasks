using DTasks.Infrastructure.Marshaling;

namespace DTasks.Serialization.Json;

internal interface IStateMachineSuspender<TStateMachine>
    where TStateMachine : notnull
{
    void Suspend(ref TStateMachine stateMachine, ISuspensionContext context, ref readonly JsonStateMachineWriter writer);
}
