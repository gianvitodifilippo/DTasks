namespace DTasks.Hosting;

internal class WhenAllContext
{
    public WhenAllContext() // For deserialization
    {
    }

    public WhenAllContext(byte remainingToComplete, FlowId parentFlowId)
    {
        RemainingToComplete = remainingToComplete;
        ParentFlowId = parentFlowId;
    }

    public byte RemainingToComplete { get; set; }

    public FlowId ParentFlowId { get; set; }
}
