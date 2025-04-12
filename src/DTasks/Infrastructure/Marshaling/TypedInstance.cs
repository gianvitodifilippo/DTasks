using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly record struct TypedInstance<TValue>(Type? Type, TValue Value)
{
    public static implicit operator TypedInstance<TValue>(TValue? value)
    {
        if (value is null)
            return default;

        Type runtimeType = value.GetType();
        if (typeof(TValue) == runtimeType)
            return new(null, value);
        
        return new(runtimeType, value);
    }
}
