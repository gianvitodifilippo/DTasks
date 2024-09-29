using System.Diagnostics.CodeAnalysis;

namespace DTasks.Hosting;

public readonly struct DTaskSuspender
{
    private readonly DTask _task;

    [ExcludeFromCodeCoverage(
#if NET8_0_OR_GREATER
        Justification = "This struct is only instantiated via Unsafe.As"
#endif
    )]
    internal DTaskSuspender(DTask task)
    {
        _task = task;
    }

    internal bool IsStateful => _task.IsStateful;

    public void SaveState<THandler>(ref THandler handler)
        where THandler : IStateHandler
    {
        _task.AssertNotRunning();
        _task.SaveState(ref handler);
    }

    public Task SuspendAsync<THandler>(ref THandler handler, CancellationToken cancellationToken = default)
        where THandler : ISuspensionHandler
    {
        _task.AssertSuspended();
        return _task.SuspendAsync(ref handler, cancellationToken);
    }
}
