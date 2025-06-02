using DTasks.Infrastructure.Generics;

namespace DTasks.Infrastructure.Marshaling;

internal sealed class DTaskTypeContext<TResult> : ITypeContext
{
    public static readonly DTaskTypeContext<TResult> Instance = new();

    private DTaskTypeContext()
    {
    }
    
    public Type Type => typeof(DTask<TResult>);

    public Type GenericType => typeof(DTask<>);

    public bool IsGeneric => true;

    public void Execute<TAction>(ref TAction action)
        where TAction : ITypeAction
    {
        action.Invoke<DTask<TResult>>();
    }

    public TReturn Execute<TAction, TReturn>(ref TAction action)
        where TAction : ITypeAction<TReturn>
    {
        return action.Invoke<DTask<TResult>>();
    }

    public void ExecuteGeneric<TAction>(ref TAction action)
        where TAction : IGenericTypeAction
    {
        action.Invoke<TResult>();
    }

    public TReturn ExecuteGeneric<TAction, TReturn>(ref TAction action)
        where TAction : IGenericTypeAction<TReturn>
    {
        return action.Invoke<TResult>();
    }
}