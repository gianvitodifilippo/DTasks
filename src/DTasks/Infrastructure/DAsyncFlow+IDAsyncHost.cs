using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure;

public sealed partial class DAsyncFlow : IDAsyncHost
{
    IDAsyncStateManager IDAsyncHost.StateManager => _host.StateManager;

    IDAsyncMarshaler IDAsyncHost.Marshaler => _host.Marshaler;

    IDAsyncTypeResolver IDAsyncHost.TypeResolver => _host.TypeResolver;

    IDAsyncCancellationProvider IDAsyncHost.CancellationProvider => _host.CancellationProvider;

    IDAsyncSuspensionHandler IDAsyncHost.SuspensionHandler => _host.SuspensionHandler;

    Task IDAsyncHost.OnStartAsync(IDAsyncFlowStartContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    Task IDAsyncHost.OnSuspendAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    Task IDAsyncHost.OnSucceedAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken) => Task.CompletedTask;

    Task IDAsyncHost.OnSucceedAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, CancellationToken cancellationToken) => Task.CompletedTask;

    Task IDAsyncHost.OnFailAsync(IDAsyncFlowCompletionContext context, Exception exception, CancellationToken cancellationToken) => Task.CompletedTask;

    Task IDAsyncHost.OnCancelAsync(IDAsyncFlowCompletionContext context, OperationCanceledException exception, CancellationToken cancellationToken) => Task.CompletedTask;
}
