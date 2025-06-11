using System.ComponentModel;

namespace DTasks.Infrastructure.Generics;

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class NonGenericTypeContext : ITypeContext
{
    public abstract Type Type { get; }
    
    public abstract bool IsStateMachine { get; }

    public Type GenericType => throw NotGeneric();

    public bool IsGeneric => false;
    
    public int Arity => throw NotGeneric();
    
    public abstract void Execute<TAction>(scoped ref TAction action)
        where TAction : ITypeAction
#if NET9_0_OR_GREATER
        , allows ref struct;
#else
        ;
#endif
    
    public abstract TReturn Execute<TAction, TReturn>(scoped ref TAction action)
        where TAction : ITypeAction<TReturn>
#if NET9_0_OR_GREATER
        , allows ref struct;
#else
        ;
#endif

    public void ExecuteGeneric<TAction>(scoped ref TAction action)
        where TAction : IGenericTypeAction
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        throw NotGeneric();
    }

    public TReturn ExecuteGeneric<TAction, TReturn>(scoped ref TAction action)
        where TAction : IGenericTypeAction<TReturn>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        throw NotGeneric();
    }
    
    private static InvalidOperationException NotGeneric() => new("Type context is not generic.");
}