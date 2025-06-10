using DTasks.Infrastructure.State;

namespace DTasks.Serialization.Json;

internal interface IStateMachineSuspender<TStateMachine>
    where TStateMachine : notnull
{
    void Suspend(ref TStateMachine stateMachine, IDehydrationContext context, ref readonly JsonStateMachineWriter writer);
}
