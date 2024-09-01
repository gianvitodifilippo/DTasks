namespace DTasks.Hosting;

public interface IStateHandler
{
    void SaveStateMachine<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo suspensionInfo)
        where TStateMachine : notnull;
}
