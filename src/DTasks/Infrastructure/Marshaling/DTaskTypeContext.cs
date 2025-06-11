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
    
    public int Arity => 1;

    public bool IsStateMachine => false;

    public void Execute<TAction>(ref TAction action)
        where TAction : ITypeAction
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        action.Invoke<DTask<TResult>>();
    }

    public TReturn Execute<TAction, TReturn>(ref TAction action)
        where TAction : ITypeAction<TReturn>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return action.Invoke<DTask<TResult>>();
    }

    public void ExecuteGeneric<TAction>(ref TAction action)
        where TAction : IGenericTypeAction
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        action.Invoke<TResult>();
    }

    public TReturn ExecuteGeneric<TAction, TReturn>(ref TAction action)
        where TAction : IGenericTypeAction<TReturn>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        return action.Invoke<TResult>();
    }
}