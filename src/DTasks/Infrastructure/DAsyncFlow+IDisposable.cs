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

    public override void Dispose()
    {
        GC.SuppressFinalize(this);
        _pool.Return(this);
    }
}