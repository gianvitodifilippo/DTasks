namespace DTasks.Hosting;

public interface IResumptionScope
{
    object GetHostReference(object token);
}
