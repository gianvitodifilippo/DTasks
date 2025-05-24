using System.ComponentModel;

namespace DTasks.Infrastructure.Generics;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ITypeAction
{
    void Invoke<T>();
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ITypeAction<out TReturn>
{
    TReturn Invoke<T>();
}