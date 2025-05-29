using System.Diagnostics.CodeAnalysis;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncFlowContext
{
    IDAsyncHostInfrastructure IDAsyncFlowContext.HostInfrastructure => _hostComponentProvider;

    bool IDAsyncFlowContext.TryGetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value)
    {
        return TryGetFlowProperty(key, out value);
    }

    private bool TryGetFlowProperty<TProperty>(DAsyncPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value)
    {
        return _flowProperties.TryGetProperty(key, out value);
    }
}