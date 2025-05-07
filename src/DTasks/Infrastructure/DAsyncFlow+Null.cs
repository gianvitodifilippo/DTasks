using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DTasks.Configuration;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow
{
    // The following saves the trouble of asserting _host and _infrastructure are not null whenever used.
    private static readonly IDAsyncInfrastructure s_nullInfrastructure = new NullDAsyncInfrastructure();
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
        private static TResult Fail<TResult>() => DAsyncFlow.Fail<TResult>(nameof(_host));

        void IDAsyncHost.OnInitialize(IDAsyncFlowInitializationContext context) => Fail<VoidDTaskResult>();

        void IDAsyncHost.OnFinalize(IDAsyncFlowFinalizationContext context) => Fail<VoidDTaskResult>();

        Task IDAsyncHost.OnStartAsync(IDAsyncFlowStartContext context, CancellationToken cancellationToken) => Fail<Task>();

        Task IDAsyncHost.OnSuspendAsync(IDAsyncFlowSuspensionContext context, CancellationToken cancellationToken) => Fail<Task>();

        Task IDAsyncHost.OnSucceedAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken) => Fail<Task>();

        Task IDAsyncHost.OnSucceedAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, CancellationToken cancellationToken) => Fail<Task>();

        Task IDAsyncHost.OnFailAsync(IDAsyncFlowCompletionContext context, Exception exception, CancellationToken cancellationToken) => Fail<Task>();

        Task IDAsyncHost.OnCancelAsync(IDAsyncFlowCompletionContext context, OperationCanceledException exception, CancellationToken cancellationToken) => Fail<Task>();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NullDAsyncInfrastructure : IDAsyncInfrastructure
    {
        private static TResult Fail<TResult>() => DAsyncFlow.Fail<TResult>(nameof(_infrastructure));

        IDAsyncTypeResolver IDAsyncInfrastructure.TypeResolver => Fail<IDAsyncTypeResolver>();

        IDAsyncHeap IDAsyncInfrastructure.Heap => Fail<IDAsyncHeap>();

        IDAsyncSurrogator IDAsyncInfrastructure.Surrogator => Fail<IDAsyncSurrogator>();

        IDAsyncCancellationProvider IDAsyncInfrastructure.CancellationProvider => Fail<IDAsyncCancellationProvider>();

        IDAsyncSuspensionHandler IDAsyncInfrastructure.SuspensionHandler => Fail<IDAsyncSuspensionHandler>();

        IDAsyncStack IDAsyncInfrastructure.GetStack(IDAsyncFlowScope scope) => Fail<IDAsyncStack>();
    }
}
