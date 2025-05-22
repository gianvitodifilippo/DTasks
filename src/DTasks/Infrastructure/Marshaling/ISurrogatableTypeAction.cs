using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ISurrogatableTypeAction
{
    void Invoke<TSurrogatable>();
}

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ISurrogatableTypeAction<out TReturn>
{
    TReturn Invoke<TSurrogatable>();
}