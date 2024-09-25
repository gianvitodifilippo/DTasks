namespace DTasks.Serialization.Json;

internal delegate void DTaskSuspender<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo info, ref readonly StateMachineDeconstructor deconstructor)
    where TStateMachine : notnull;
