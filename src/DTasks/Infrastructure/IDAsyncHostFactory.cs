namespace DTasks.Infrastructure;

public interface IDAsyncHostFactory
{
    IDAsyncHost CreateHost(IDAsyncHostCreationContext context);
}
