using DTasks.Execution;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure;

public sealed partial class DAsyncFlow
{
    // Since _host is initialized in the entrypoints and defaulted only when calling Reset,
    // the following saves the trouble of asserting it's not null whenever it's used.
    private static readonly IDAsyncHost s_nullHost = new NullDAsyncHost();

    [ExcludeFromCodeCoverage]
    private sealed class NullDAsyncHost : IDAsyncHost
    {
        [DoesNotReturn]
        private static TResult Fail<TResult>()
        {
            Debug.Fail($"'{nameof(_host)}' was not initialized.");
            throw new UnreachableException();
        }
        
        IDAsyncStateManager IDAsyncHost.StateManager => Fail<IDAsyncStateManager>();

        IDAsyncMarshaler IDAsyncHost.Marshaler => Fail<IDAsyncMarshaler>();

        IDAsyncTypeResolver IDAsyncHost.TypeResolver => Fail<IDAsyncTypeResolver>();

        IDAsyncCancellationProvider IDAsyncHost.CancellationProvider => Fail<IDAsyncCancellationProvider>();

        IDAsyncSuspensionHandler IDAsyncHost.SuspensionHandler => Fail<IDAsyncSuspensionHandler>();

        Task IDAsyncHost.OnStartAsync(IDAsyncResultBuilder resultBuilder, CancellationToken cancellationToken) => Fail<Task>();

        Task IDAsyncHost.OnSuspendAsync(CancellationToken cancellationToken) => Fail<Task>();

        Task IDAsyncHost.OnSucceedAsync(CancellationToken cancellationToken) => Fail<Task>();

        Task IDAsyncHost.OnSucceedAsync<TResult>(TResult result, CancellationToken cancellationToken) => Fail<Task>();

        Task IDAsyncHost.OnFailAsync(Exception exception, CancellationToken cancellationToken) => Fail<Task>();

        Task IDAsyncHost.OnCancelAsync(OperationCanceledException exception, CancellationToken cancellationToken) => Fail<Task>();
    }
}
