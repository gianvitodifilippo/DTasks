using System.Diagnostics.CodeAnalysis;

namespace DTasks.Infrastructure;

internal sealed partial class DAsyncFlow : IDAsyncFlowFinalizationContext
{
    bool IDAsyncFlowFinalizationContext.TryGetProperty<TProperty>(DAsyncFlowPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value)
    {
        return TryGetProperty(key, out value);
    }

    bool IDAsyncFlowFinalizationContext.RemoveProperty<TProperty>(DAsyncFlowPropertyKey<TProperty> key, [MaybeNullWhen(false)] out TProperty value)
    {
        if (!_properties.Remove(key.Key, out object? untypedValue))
        {
            value = default;
            return false;
        }

        if (untypedValue is not TProperty typedValue)
        {
            // They passed a key with the wrong type, add the value back in the dictionary.
            // This assumes that this is less common than passing the right key type, as the alternative approach
            // would be do a first lookup to determine whether the type is correct 
            _properties.Add(key.Key, untypedValue);

            value = default;
            return false;
        }

        value = typedValue;
        return true;
    }
}
