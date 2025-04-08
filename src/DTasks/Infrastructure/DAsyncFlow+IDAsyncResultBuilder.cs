using System.Diagnostics;

namespace DTasks.Infrastructure;

public sealed partial class DAsyncFlow : IDAsyncResultBuilder
{
    private static readonly object s_resultSentinel = new();

    void IDAsyncResultBuilder.SetResult() => SetResultOrException(s_resultSentinel);

    void IDAsyncResultBuilder.SetException(Exception exception) => SetResultOrException(exception);

    private void SetResultOrException(object resultOrException)
    {
        Debug.Assert(_state is FlowState.Starting, $"{nameof(IDAsyncResultBuilder)} should be exposed only when starting.");
        
        if (_resultOrException is not null)
            throw new InvalidOperationException("The result was already set.");
        
        _resultOrException = resultOrException;
    }
}