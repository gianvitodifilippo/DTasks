using System.ComponentModel;

namespace DTasks.Infrastructure.Generics;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ITypeContext
{
    Type Type { get; }
    
    Type GenericType { get; }
    
    bool IsGeneric { get; }
    
    bool IsStateMachine { get; }
    
    void Execute<TAction>(scoped ref TAction action)
        where TAction : ITypeAction
#if NET9_0_OR_GREATER
        , allows ref struct;
#else
    ;
#endif
    
    TReturn Execute<TAction, TReturn>(scoped ref TAction action)
        where TAction : ITypeAction<TReturn>
#if NET9_0_OR_GREATER
        , allows ref struct;
#else
    ;
#endif
    
    void ExecuteGeneric<TAction>(scoped ref TAction action)
        where TAction : IGenericTypeAction
#if NET9_0_OR_GREATER
        , allows ref struct;
#else
    ;
#endif
    
    TReturn ExecuteGeneric<TAction, TReturn>(scoped ref TAction action)
        where TAction : IGenericTypeAction<TReturn>
#if NET9_0_OR_GREATER
        , allows ref struct;
#else
    ;
#endif
}
