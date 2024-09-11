namespace DTasks.Hosting;

public class CorruptedDFlowException : Exception
{
    public CorruptedDFlowException(object flowId)
        : base(CreateDefaultMessage(flowId))
    {
        FlowId = flowId;
    }

    public CorruptedDFlowException(object flowId, Exception innerException)
        : base(CreateDefaultMessage(flowId), innerException)
    {
        FlowId = flowId;
    }

    public CorruptedDFlowException(object flowId, string message)
        : base(message)
    {
        FlowId = flowId;
    }

    public CorruptedDFlowException(object flowId, string message, Exception innerException)
        : base(message, innerException)
    {
        FlowId = flowId;
    }

    public object FlowId { get; }

    private static string CreateDefaultMessage(object flowId)
    {
        return $"The state of distributed flow with id '{flowId}' was was corrupted.";
    }
}
