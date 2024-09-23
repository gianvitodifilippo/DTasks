using System.Diagnostics;

namespace DTasks.Extensions.Microsoft.DependencyInjection.Hosting;

internal class KeyedServiceToken<TKey> : ServiceToken, IKeyedServiceToken
{
    protected KeyedServiceToken() { }

    public TKey? Key { get; set; }

    object? IKeyedServiceToken.Key
    {
        get => Key;
        set
        {
            Debug.Assert(value is null && !typeof(TKey).IsValueType || value is TKey, "Invalid key type.");
            Key = (TKey?)value;
        }
    }
}
