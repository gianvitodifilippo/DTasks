namespace DTasks.Hosting;

public interface IStateMachineInfo
{
    Type SuspendedAwaiterType { get; }
}
