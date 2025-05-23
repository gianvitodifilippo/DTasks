using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class SurrogatableTypeContext
{
    public static ISurrogatableTypeContext Of<TSurrogatable>() => SurrogatableTypeContext<TSurrogatable>.Instance;
}

internal sealed class SurrogatableTypeContext<TSurrogatable> : ISurrogatableTypeContext
{
    public static readonly SurrogatableTypeContext<TSurrogatable> Instance = new();
    
    private SurrogatableTypeContext()
    {
    }
    
    public Type Type => typeof(TSurrogatable);

    public void Execute<TAction>(ref TAction action)
        where TAction : ISurrogatableTypeAction
    {
        action.Invoke<TSurrogatable>();
    }

    public TReturn Execute<TAction, TReturn>(ref TAction action)
        where TAction : ISurrogatableTypeAction<TReturn>
    {
        return action.Invoke<TSurrogatable>();
    }
}