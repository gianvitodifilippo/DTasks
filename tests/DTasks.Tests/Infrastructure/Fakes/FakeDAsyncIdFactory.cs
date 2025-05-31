using Xunit.Sdk;

namespace DTasks.Infrastructure.Fakes;

internal sealed class FakeDAsyncIdFactory : DAsyncIdFactory
{
    public static readonly DAsyncId TestRootId = DAsyncId.Parse("g000000000000000");
    
    private bool _flowStarted;
    private bool _madeFlowId;
    private int _index;

    public DAsyncId GetTestId(int index)
    {
        if (_flowStarted)
            throw FailException.ForFailure("Flow already started.");
        
        if (index >= 10000)
            throw FailException.ForFailure("Too many ids generated.");
        
        return DAsyncId.Parse($"A00000000000{index:D4}");
    }
    
    public override DAsyncId NewId()
    {
        _flowStarted = true;

        _index++;
        return DAsyncId.Parse($"A00000000000{_index:D4}");
    }

    public override DAsyncId NewFlowId()
    {
        if (_madeFlowId)
            throw FailException.ForFailure("Only one flow id per test.");
        
        _madeFlowId = true;
        return TestRootId;
    }
}