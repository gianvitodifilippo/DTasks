using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Infrastructure;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncHost
{
    bool TryGetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value);
    
    void OnInitialize(IDAsyncFlowInitializationContext context);

    void OnFinalize(IDAsyncFlowFinalizationContext context);

    Task OnStartAsync(IDAsyncFlowStartContext context, CancellationToken cancellationToken);

    Task OnSuspendAsync(IDAsyncFlowSuspensionContext context, CancellationToken cancellationToken);

    Task OnSucceedAsync(IDAsyncFlowCompletionContext context, CancellationToken cancellationToken);

    Task OnSucceedAsync<TResult>(IDAsyncFlowCompletionContext context, TResult result, CancellationToken cancellationToken);

    Task OnFailAsync(IDAsyncFlowCompletionContext context, Exception exception, CancellationToken cancellationToken);

    Task OnCancelAsync(IDAsyncFlowCompletionContext context, OperationCanceledException exception, CancellationToken cancellationToken);
}