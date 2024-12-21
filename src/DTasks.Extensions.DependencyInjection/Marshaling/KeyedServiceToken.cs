using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Extensions.DependencyInjection.Marshaling;

internal class KeyedServiceToken<TKey> : ServiceToken, IKeyedServiceToken
    where TKey : notnull
{
    [DisallowNull]
    public TKey? Key { get; set; }

    [DisallowNull]
    object? IKeyedServiceToken.Key
    {
        get => Key;
        set
        {
            Debug.Assert(value is TKey, "Invalid key type.");
            Key = (TKey)value;
        }
    }
}
