using DTasks.Execution;
using DTasks.Infrastructure;
using DTasks.Infrastructure.Execution;

namespace DTasks;

internal sealed class DelegateSuspensionDTask<TResult>(SuspensionCallback callback) : DTask<TResult>, ISuspensionCallback
{
    private bool _executed;
    
    public override DTaskStatus Status => DTaskStatus.Suspended;

    public Task InvokeAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        if (_executed)
            throw new InvalidOperationException("Attempted to execute a suspension callback more than once.");
        
        _executed = true;
        return callback(id, cancellationToken);
    }

    protected override void Run(IDAsyncRunner runner)
    {
        ISuspensionFeature suspensionFeature = runner.Features.GetRequiredFeature<ISuspensionFeature>();
        suspensionFeature.Suspend(this);
    }
}

internal sealed class DelegateSuspensionDTask<TState, TResult>(TState state, SuspensionCallback<TState> callback) : DTask<TResult>, ISuspensionCallback
{
    private bool _executed;
    
    public override DTaskStatus Status => DTaskStatus.Suspended;

    public Task InvokeAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        if (_executed)
            throw new InvalidOperationException("Attempted to execute a suspension callback more than once.");
        
        _executed = true;
        return callback(id, state, cancellationToken);
    }

    protected override void Run(IDAsyncRunner runner)
    {
        ISuspensionFeature suspensionFeature = runner.Features.GetRequiredFeature<ISuspensionFeature>();
        suspensionFeature.Suspend(this);
    }
}
