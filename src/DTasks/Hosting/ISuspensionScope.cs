using DTasks.Serialization;

namespace DTasks.Hosting;

public interface ISuspensionScope
{
    void InitializeHeap<THeap>(ref THeap heap)
        where THeap : IFlowHeap;
}
