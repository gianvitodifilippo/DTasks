namespace DTasks.Marshaling;

public class MarshalingException : Exception
{
    private const string DefaultMessage = "An error occured during marshaling.";

    public MarshalingException()
        : base(DefaultMessage)
    {
    }

    public MarshalingException(string? message)
        : base(message ?? DefaultMessage)
    {
    }

    public MarshalingException(string? message, Exception? innerException)
        : base(message ?? DefaultMessage, innerException)
    {
    }
}