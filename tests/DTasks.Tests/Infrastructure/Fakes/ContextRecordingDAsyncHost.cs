using System.Diagnostics.CodeAnalysis;

namespace DTasks.Infrastructure.Fakes;

internal sealed class ContextRecordingDAsyncHost(IDAsyncHost host) : IDAsyncHost
{
    bool IDAsyncHost.TryGetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value)
    {
        return host.TryGetProperty(key, out value);
    }

    void IDAsyncHost.OnInitialize(IDAsyncFlowInitializationContext context)
    {
        host.OnInitialize(context);
    }

    void IDAsyncHost.OnFinalize(IDAsyncFlowFinalizationContext context)
    {
        host.OnFinalize(context);
    }

    Task IDAsyncHost.OnStartAsync(IDAsyncFlowStartContext context, CancellationToken cancellationToken)
    {
        return host.OnStartAsync(new DAsyncFlowStartContext(context), cancellationToken);
    }

    Task IDAsyncHost.OnSuspendAsync(IDAsyncFlowSuspensionContext context, CancellationToken cancellationToken)
    {
        return host.OnSuspendAsync(context, cancellationToken);
    }

    Task IDAsyncHost.OnSucceedAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken)
    {
        return host.OnSucceedAsync(new DAsyncFlowCompletionContext(context), cancellationToken);
    }

    Task IDAsyncHost.OnSucceedAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, CancellationToken cancellationToken)
    {
        return host.OnSucceedAsync(new DAsyncFlowCompletionContext(context), result, cancellationToken);
    }

    Task IDAsyncHost.OnFailAsync(IDAsyncFlowCompletionContext context, Exception exception, CancellationToken cancellationToken)
    {
        return host.OnFailAsync(new DAsyncFlowCompletionContext(context), exception, cancellationToken);
    }

    Task IDAsyncHost.OnCancelAsync(IDAsyncFlowCompletionContext context, OperationCanceledException exception, CancellationToken cancellationToken)
    {
        return host.OnCancelAsync(new DAsyncFlowCompletionContext(context), exception, cancellationToken);
    }
    
    private sealed class DAsyncFlowStartContext(IDAsyncFlowStartContext context) : IDAsyncFlowStartContext
    {
        public IDAsyncHostInfrastructure HostInfrastructure => context.HostInfrastructure;

        public bool TryGetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value)
        {
            return context.TryGetProperty(key, out value);
        }

        public void SetResult()
        {
            context.SetResult();
        }

        public void SetException(Exception exception)
        {
            context.SetException(exception);
        }

        public DAsyncId FlowId { get; } = context.FlowId;
    }
    
    private sealed class DAsyncFlowCompletionContext(IDAsyncFlowCompletionContext context) : IDAsyncFlowCompletionContext
    {
        public IDAsyncHostInfrastructure HostInfrastructure => context.HostInfrastructure;

        public bool TryGetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value)
        {
            return context.TryGetProperty(key, out value);
        }

        public DAsyncId FlowId { get; } = context.FlowId;
    }
}