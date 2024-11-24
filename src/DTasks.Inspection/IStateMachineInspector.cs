namespace DTasks.Inspection;

public interface IStateMachineInspector
{
    object GetConverter(Type stateMachineType);
}
