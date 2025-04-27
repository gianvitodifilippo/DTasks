using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly record struct TypedInstance<TValue>(Type? Type, TValue Instance)
{
    public TypedInstance(TValue instance)
        : this(null, instance)
    {
    }

    public static implicit operator TypedInstance<TValue>(TValue? instance) => TypedInstance.From(instance);
}

[EditorBrowsable(EditorBrowsableState.Never)]
public static class TypedInstance
{
    public static TypedInstance<TValue> From<TValue>(TValue? instance)
    {
        if (instance is null)
            return default;

        Type runtimeType = instance.GetType();
        if (typeof(TValue) == runtimeType)
            return new(null, instance);

        return new(runtimeType, instance);
    }

    public static TypedInstance<TValue> Untyped<TValue>(TValue? instance)
    {
        if (instance is null)
            return default;

        return new(null, instance);
    }
}