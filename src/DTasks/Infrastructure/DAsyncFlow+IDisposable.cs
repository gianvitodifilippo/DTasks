using System.Diagnostics;

namespace DTasks.Infrastructure;

public sealed partial class DAsyncFlow : IDisposable
{
#if DEBUG
    ~DAsyncFlow()
    {
        if (_state is FlowState.Idling)
            return;

        Debug.WriteLine($"An instance of {nameof(DAsyncFlow)} was not disposed. Created at:{Environment.NewLine}{_stackTrace}");
    }
#endif
    
    public void Dispose()
    {
        ReturnToCache();

#if DEBUG
        GC.SuppressFinalize(this);
#endif
    }
}