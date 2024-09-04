namespace DTasks.Serialization;

public interface IFlowHeap
{
    void AddHostReference<TToken>(object reference, TToken token);
}
