using System.Diagnostics.CodeAnalysis;
using DTasks.Configuration;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncScope
{
    DTasksConfiguration IDAsyncFlow.Configuration => _host.Configuration;

    IDAsyncSurrogator IDAsyncFlow.Surrogator => this;
    
    bool IDAsyncFlow.TryGetProperty<TProperty>(DAsyncFlowPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value)
    {
        if (_properties.TryGetValue(key.Key, out object? untypedValue) && untypedValue is TProperty typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }

    bool IDAsyncScope.TryGetComponent<TComponent>(object key, [NotNullWhen(true)] out TComponent? component)
        where TComponent : default
    {
        if (_properties.TryGetValue(key, out object? untypedComponent))
        {
            component = (TComponent)untypedComponent!;
            return true;
        }

        component = default;
        return false;
    }

    void IDAsyncScope.AddComponent<TComponent>(object key, TComponent component)
    {
        _properties.Add(key, component);
    }
}
