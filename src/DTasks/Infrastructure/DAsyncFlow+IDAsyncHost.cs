using DTasks.Configuration;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncHost
{
    DTasksConfiguration IDAsyncHost.Configuration => Configuration;

    void IDAsyncHost.OnInitialize(IDAsyncFlowInitializationContext context) { }

    void IDAsyncHost.OnFinalize(IDAsyncFlowFinalizationContext context) { }

    Task IDAsyncHost.OnStartAsync(IDAsyncFlowStartContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    Task IDAsyncHost.OnSuspendAsync(IDAsyncFlowSuspensionContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    Task IDAsyncHost.OnSucceedAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    Task IDAsyncHost.OnSucceedAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, CancellationToken cancellationToken) => Task.CompletedTask;

    Task IDAsyncHost.OnFailAsync(IDAsyncFlowCompletionContext context, Exception exception, CancellationToken cancellationToken) => Task.CompletedTask;

    Task IDAsyncHost.OnCancelAsync(IDAsyncFlowCompletionContext context, OperationCanceledException exception, CancellationToken cancellationToken) => Task.CompletedTask;
}
