using System.ComponentModel;

namespace DTasks.Infrastructure.Generics;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class TypeContext
{
    public static ITypeContext Of<T>() => TypeContext<T>.Instance;
}

internal sealed class TypeContext<T> : ITypeContext
{
    public static readonly TypeContext<T> Instance = new();
    
    private TypeContext()
    {
    }
    
    public Type Type => typeof(T);
    
    public Type GenericType => throw NotGeneric();

    public bool IsGeneric => false;

    public void Execute<TAction>(ref TAction action)
        where TAction : ITypeAction
    {
        action.Invoke<T>();
    }

    public TReturn Execute<TAction, TReturn>(ref TAction action)
        where TAction : ITypeAction<TReturn>
    {
        return action.Invoke<T>();
    }

    public void ExecuteGeneric<TAction>(ref TAction action)
        where TAction : IGenericTypeAction
    {
        throw NotGeneric();
    }

    public TReturn ExecuteGeneric<TAction, TReturn>(ref TAction action)
        where TAction : IGenericTypeAction<TReturn>
    {
        throw NotGeneric();
    }
    
    private static InvalidOperationException NotGeneric() => new InvalidOperationException("Type context is not generic.");
}