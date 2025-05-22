namespace DTasks.Infrastructure.Marshaling;

internal sealed class SurrogatableTypeContext<TSurrogatable> : ISurrogatableTypeContext
{
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