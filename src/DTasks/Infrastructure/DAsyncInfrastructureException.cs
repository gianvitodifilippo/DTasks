namespace DTasks.Infrastructure;

public class DAsyncInfrastructureException : Exception
{
    private const string DefaultMessage = "An exception occurred within an infrastructure component.";
    
    public DAsyncInfrastructureException()
        : base(DefaultMessage)
    {
    }

    public DAsyncInfrastructureException(string? message)
        : base(message ?? DefaultMessage)
    {
    }

    public DAsyncInfrastructureException(string? message, Exception? innerException)
        : base(message ?? DefaultMessage, innerException)
    {
    }
}