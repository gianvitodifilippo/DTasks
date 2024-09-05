namespace DTasks.Inspection;

public interface IStateMachineInspector
{
    Delegate GetSuspender(Type stateMachineType);

    Delegate GetResumer(Type stateMachineType);
}
