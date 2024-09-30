namespace DTasks.Hosting;

internal class WhenAllContext
{
    public WhenAllContext() // For deserialization
    {
    }

    public WhenAllContext(HashSet<byte> branchIndexes, FlowId parentFlowId)
    {
        BranchIndexes = branchIndexes;
        ParentFlowId = parentFlowId;
    }

    public HashSet<byte>? BranchIndexes { get; set; }

    public FlowId ParentFlowId { get; set; }
}
