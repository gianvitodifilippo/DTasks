namespace DTasks.Serialization;

public interface IStateMachineTypeResolver
{
    Type GetType(object typeId);

    object GetTypeId(Type type);
}
