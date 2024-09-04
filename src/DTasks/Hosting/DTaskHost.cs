using System.Runtime.CompilerServices;

namespace DTasks.Hosting;

public abstract class DTaskHost<TFlowId>
    where TFlowId : notnull
{
    public Task SuspendAsync(TFlowId flowId, ISuspensionScope scope, DTask.DAwaiter awaiter, CancellationToken cancellationToken = default)
    {
        return SuspendCoreAsync(flowId, scope, awaiter, cancellationToken);
    }

    public Task SuspendAsync<TResult>(TFlowId flowId, ISuspensionScope scope, DTask<TResult>.DAwaiter awaiter, CancellationToken cancellationToken = default)
    {
        return SuspendCoreAsync(flowId, scope, Unsafe.As<DTask<TResult>.DAwaiter, DTask.DAwaiter>(ref awaiter), cancellationToken);
    }

    public Task ResumeAsync(TFlowId flowId, IResumptionScope scope, CancellationToken cancellationToken = default)
    {
        return ResumeCoreAsync(flowId, scope, DTask.CompletedTask, cancellationToken);
    }

    public Task ResumeAsync<TResult>(TFlowId flowId, IResumptionScope scope, TResult result, CancellationToken cancellationToken = default)
    {
        return ResumeCoreAsync(flowId, scope, DTask.FromResult(result), cancellationToken);
    }

    protected abstract Task SuspendCoreAsync(TFlowId flowId, ISuspensionScope scope, DTask.DAwaiter awaiter, CancellationToken cancellationToken);
    
    protected abstract Task ResumeCoreAsync(TFlowId flowId, IResumptionScope scope, DTask resultTask, CancellationToken cancellationToken);
}
