namespace DTasks.Hosting;

public interface IStateMachineInfo
{
    bool IsSuspended(Type awaiterType);
}
