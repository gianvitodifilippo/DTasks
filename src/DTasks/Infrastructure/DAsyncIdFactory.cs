namespace DTasks.Infrastructure;

internal class DAsyncIdFactory
{
    public static readonly DAsyncIdFactory Default = new();
    
    public virtual DAsyncId NewId() => DAsyncId.New();

    public virtual DAsyncId NewFlowId() => DAsyncId.NewFlow();
}