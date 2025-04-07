using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly record struct TypedInstance<T>(TypeId TypeId, T Value)
{
    public static implicit operator TypedInstance<T>(T value) => new(default, value);
}
