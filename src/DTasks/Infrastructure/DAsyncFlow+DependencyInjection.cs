using System.Diagnostics.CodeAnalysis;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncHostScope, IDAsyncFlowScope
{
    IDAsyncRootScope IDAsyncHostScope.Parent => _hostComponentProvider.RootScope;

    IDAsyncHostScope IDAsyncFlowScope.Parent => _flowComponentProvider.HostScope;

    IDAsyncSurrogator IDAsyncFlowScope.Surrogator => this;

    bool IDAsyncHostScope.TryGetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value)
    {
        return _host.TryGetProperty(key, out value);
    }
    
    bool IDAsyncFlowScope.TryGetProperty<TProperty>(DAsyncPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value)
    {
        return TryGetFlowProperty(key, out value);
    }
}