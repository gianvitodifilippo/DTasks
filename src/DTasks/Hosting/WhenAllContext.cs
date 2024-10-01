namespace DTasks.Hosting;

internal class WhenAllContext
{
    public WhenAllContext() // For deserialization
    {
    }

    public WhenAllContext(FlowId parentFlowId, HashSet<byte> branches)
    {
        ParentFlowId = parentFlowId;
        Branches = branches;
    }

    public HashSet<byte>? Branches { get; set; }

    public FlowId ParentFlowId { get; set; }
}

internal class WhenAllContext<TResult>
{
    public WhenAllContext() // For deserialization
    {
    }

    public WhenAllContext(FlowId parentFlowId, Dictionary<byte, TResult> branches, byte branchCount)
    {
        Branches = branches;
        ParentFlowId = parentFlowId;
        BranchCount = branchCount;
    }

    public byte BranchCount { get; set; }

    public Dictionary<byte, TResult>? Branches { get; set; }

    public FlowId ParentFlowId { get; set; }
}
