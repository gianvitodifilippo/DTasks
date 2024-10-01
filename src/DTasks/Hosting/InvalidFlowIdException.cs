namespace DTasks.Hosting;

public class InvalidFlowIdException : Exception
{
    public InvalidFlowIdException(FlowId flowId)
        : base($"Flow '{flowId}' does not exist or has already completed.")
    {
        FlowId = flowId;
    }

    public FlowId FlowId { get; }
}
