using System.Runtime.CompilerServices;

namespace DTasks.Hosting;

public abstract class DTaskHost<TFlowId, TContext>
    where TFlowId : notnull
{
    public Task SuspendAsync(TFlowId flowId, TContext context, IDTaskScope scope, DTask.DAwaiter awaiter, CancellationToken cancellationToken = default)
    {
        return SuspendCoreAsync(flowId, context, scope, awaiter, cancellationToken);
    }

    public Task SuspendAsync<TResult>(TFlowId flowId, TContext context, IDTaskScope scope, DTask<TResult>.DAwaiter awaiter, CancellationToken cancellationToken = default)
    {
        return SuspendCoreAsync(flowId, context, scope, Unsafe.As<DTask<TResult>.DAwaiter, DTask.DAwaiter>(ref awaiter), cancellationToken);
    }

    public Task ResumeAsync(TFlowId flowId, IDTaskScope scope, CancellationToken cancellationToken = default)
    {
        return ResumeCoreAsync(flowId, scope, DTask.CompletedTask, cancellationToken);
    }

    public Task ResumeAsync<TResult>(TFlowId flowId, IDTaskScope scope, TResult result, CancellationToken cancellationToken = default)
    {
        return ResumeCoreAsync(flowId, scope, DTask.FromResult(result), cancellationToken);
    }

    protected abstract Task SuspendCoreAsync(TFlowId flowId, TContext context, IDTaskScope scope, DTask.DAwaiter awaiter, CancellationToken cancellationToken);

    protected abstract Task ResumeCoreAsync(TFlowId flowId, IDTaskScope scope, DTask resultTask, CancellationToken cancellationToken);
}
