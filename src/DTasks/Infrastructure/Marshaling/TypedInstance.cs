using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly record struct TypedInstance<T>(Type? Type, T Value)
{
    public static implicit operator TypedInstance<T>(T value)
    {
        if (value is null)
            return new(null, value);

        Type runtimeType = value.GetType();
        if (typeof(T) == runtimeType)
            return new(null, value);
        
        return new(runtimeType, value);
    }
}
