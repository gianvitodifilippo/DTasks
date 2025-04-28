namespace DTasks.Infrastructure;

public interface IDAsyncHostFactory
{
    IDAsyncHost CreateHost(IDAsyncFlow flow);
}
