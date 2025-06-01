using DTasks.Execution;
using DTasks.Infrastructure;
using DTasks.Infrastructure.Execution;

namespace DTasks;

internal sealed class SuspensionDTask<TCallback, TResult>(TCallback callback) : DTask<TResult>, ISuspensionCallback
    where TCallback : ISuspensionCallback
{
    private bool _executed;
    private TCallback _callback = callback;

    public override DTaskStatus Status => DTaskStatus.Suspended;

    public Task InvokeAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        if (_executed)
            throw new InvalidOperationException("Attempted to execute a suspension callback more than once.");
        
        _executed = true;
        return _callback.InvokeAsync(id, cancellationToken);
    }

    protected override void Run(IDAsyncRunner runner)
    {
        ISuspensionFeature suspensionFeature = runner.Features.GetRequiredFeature<ISuspensionFeature>();
        suspensionFeature.Suspend(this);
    }
}
