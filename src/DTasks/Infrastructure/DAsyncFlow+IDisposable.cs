using System.Diagnostics;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
#if DEBUG
    ~DAsyncFlow()
    {
        if (_state is FlowState.Idling)
            return;

        Debug.WriteLine($"An instance of {nameof(DAsyncRunner)} was not disposed. Created at:{Environment.NewLine}{_stackTrace}");
    }
#endif
    
    public override void Dispose() => ReturnToCache();
}