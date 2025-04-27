using DTasks.Execution;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DTasks.Configuration;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
    // Since _host is initialized in the entrypoints and defaulted only when calling Reset,
    // the following saves the trouble of asserting it's not null whenever used.
    private static readonly IDAsyncHost s_nullHost = new NullDAsyncHost();
    
    [DoesNotReturn]
    private static TResult Fail<TResult>(string name)
    {
        Debug.Fail($"'{name}' was not initialized.");
        throw new UnreachableException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NullDAsyncHost : IDAsyncHost
    {
        [DoesNotReturn]
        private static TResult Fail<TResult>() => DAsyncFlow.Fail<TResult>(nameof(_host));

        DTasksConfiguration IDAsyncHost.Configuration => Fail<DTasksConfiguration>();

        void IDAsyncHost.OnInitialize(IDAsyncFlowInitializationContext context) => Fail<VoidDTaskResult>();

        void IDAsyncHost.OnFinalize(IDAsyncFlowFinalizationContext context) => Fail<VoidDTaskResult>();

        Task IDAsyncHost.OnStartAsync(IDAsyncFlowStartContext context, CancellationToken cancellationToken) => Fail<Task>();

        Task IDAsyncHost.OnSuspendAsync(IDAsyncFlowSuspensionContext context, CancellationToken cancellationToken) => Fail<Task>();

        Task IDAsyncHost.OnSucceedAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken) => Fail<Task>();

        Task IDAsyncHost.OnSucceedAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, CancellationToken cancellationToken) => Fail<Task>();

        Task IDAsyncHost.OnFailAsync(IDAsyncFlowCompletionContext context, Exception exception, CancellationToken cancellationToken) => Fail<Task>();

        Task IDAsyncHost.OnCancelAsync(IDAsyncFlowCompletionContext context, OperationCanceledException exception, CancellationToken cancellationToken) => Fail<Task>();
    }
}
