using System.Diagnostics;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
#if DEBUG
    ~DAsyncFlow()
    {
        Debug.WriteLine($"An instance of {nameof(DAsyncRunner)} was not disposed. Created at:{Environment.NewLine}{_stackTrace}");
    }
#endif

    protected override void Dispose(bool disposing)
    {
        _pool.Return(this);
        _state = FlowState.Idling;
        _host = s_nullHost;
        _hostComponentProvider.Reset();
        
#if DEBUG
        _stackTrace = null;
#endif
    }
}