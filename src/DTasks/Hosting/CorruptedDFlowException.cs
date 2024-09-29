namespace DTasks.Hosting;

public class CorruptedDFlowException : Exception
{
    public const string RethrowableSource = "DTasks.Hosting.CorruptedDFlowException";

    public CorruptedDFlowException(FlowId flowId)
        : base(CreateDefaultMessage(flowId))
    {
        FlowId = flowId;
    }

    public CorruptedDFlowException(FlowId flowId, Exception? innerException)
        : base(CreateDefaultMessage(flowId), innerException)
    {
        FlowId = flowId;
    }

    public CorruptedDFlowException(FlowId flowId, string? message)
        : base(message ?? CreateDefaultMessage(flowId))
    {
        FlowId = flowId;
    }

    public CorruptedDFlowException(FlowId flowId, string? message, Exception? innerException)
        : base(message ?? CreateDefaultMessage(flowId), innerException)
    {
        FlowId = flowId;
    }

    public FlowId FlowId { get; }

    private static string CreateDefaultMessage(FlowId flowId)
    {
        return $"The state of distributed flow with id '{flowId}' was was corrupted.";
    }

    internal static void ThrowIfRethrowable(FlowId id, Exception ex)
    {
        if (ex.Source != RethrowableSource)
            return;

        string? message = ex.Message;
        if (message == string.Empty)
        {
            message = null;
        }

        throw new CorruptedDFlowException(id, message, ex.InnerException);
    }
}
