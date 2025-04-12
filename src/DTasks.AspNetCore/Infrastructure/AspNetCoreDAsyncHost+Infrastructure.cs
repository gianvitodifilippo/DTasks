using System.Diagnostics.CodeAnalysis;
using DTasks.Infrastructure;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.AspNetCore.Infrastructure;

public abstract partial class AspNetCoreDAsyncHost : DAsyncHost
{
    private IDAsyncMarshaler? _marshaler;
    private IDAsyncStateManager? _stateManager;
    private IDAsyncTypeResolver? _typeResolver;
    // private IDAsyncCancellationProvider? _cancellationProvider;
    private IDAsyncSuspensionHandler? _suspensionHandler;

    protected override IDAsyncMarshaler Marshaler => GetService(ref _marshaler);

    protected override IDAsyncStateManager StateManager => GetService(ref _stateManager);

    protected override IDAsyncTypeResolver TypeResolver => GetService(ref _typeResolver);

    // protected override IDAsyncCancellationProvider CancellationProvider => GetService(ref _cancellationProvider);

    protected override IDAsyncSuspensionHandler SuspensionHandler => GetService(ref _suspensionHandler);

    private TService GetService<TService>([NotNull] ref TService? service)
        where TService : notnull
    {
        return service ??= Services.GetRequiredService<TService>();
    }
}
