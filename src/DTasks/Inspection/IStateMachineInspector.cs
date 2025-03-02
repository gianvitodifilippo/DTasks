namespace DTasks.Inspection;

public interface IStateMachineInspector
{
    object GetSuspender(Type stateMachineType);

    object GetResumer(Type stateMachineType);
}
