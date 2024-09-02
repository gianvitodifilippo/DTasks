using System.Runtime.CompilerServices;

namespace DTasks.Hosting;

public abstract class DTaskHost<TFlowId>
    where TFlowId : notnull
{
    public Task SuspendAsync(TFlowId flowId, IDTaskScope scope, DTask.DAwaiter awaiter, CancellationToken cancellationToken = default)
    {
        return SuspendCoreAsync(flowId, scope, awaiter, cancellationToken);
    }

    public Task SuspendAsync<TResult>(TFlowId flowId, IDTaskScope scope, DTask<TResult>.DAwaiter awaiter, CancellationToken cancellationToken = default)
    {
        return SuspendCoreAsync(flowId, scope, Unsafe.As<DTask<TResult>.DAwaiter, DTask.DAwaiter>(ref awaiter), cancellationToken);
    }

    public Task ResumeAsync(TFlowId flowId, IDTaskScope scope, CancellationToken cancellationToken = default)
    {
        return ResumeCoreAsync(flowId, DTask.CompletedTask, scope, cancellationToken);
    }

    public Task ResumeAsync<TResult>(TFlowId flowId, TResult result, IDTaskScope scope, CancellationToken cancellationToken = default)
    {
        return ResumeCoreAsync(flowId, DTask.FromResult(result), scope, cancellationToken);
    }

    protected abstract Task SuspendCoreAsync(TFlowId flowId, IDTaskScope scope, DTask.DAwaiter awaiter, CancellationToken cancellationToken);
    
    protected abstract Task ResumeCoreAsync(TFlowId flowId, DTask resultTask, IDTaskScope scope, CancellationToken cancellationToken);
}
