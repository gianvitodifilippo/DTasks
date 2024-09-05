namespace DTasks.Hosting;

public interface IStateHandler
{
    void SaveStateMachine<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo info)
        where TStateMachine : notnull;
}
