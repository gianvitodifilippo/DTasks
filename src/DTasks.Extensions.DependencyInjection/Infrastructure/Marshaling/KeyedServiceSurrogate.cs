using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Extensions.DependencyInjection.Infrastructure.Marshaling;

internal class KeyedServiceSurrogate<TKey> : ServiceSurrogate, IKeyedServiceSurrogate
    where TKey : notnull
{
    [DisallowNull]
    public TKey? Key { get; set; }

    [DisallowNull]
    object? IKeyedServiceSurrogate.Key
    {
        get => Key;
        set
        {
            Debug.Assert(value is TKey, "Invalid key type.");
            Key = (TKey)value;
        }
    }
}
