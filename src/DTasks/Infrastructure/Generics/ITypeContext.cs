using System.ComponentModel;
using DTasks.Utils;

namespace DTasks.Infrastructure.Generics;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ITypeContext
{
    Type Type { get; }
    
    Type GenericType { get; }
    
    bool IsGeneric { get; }
    
    int Arity { get; }
    
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

[EditorBrowsable(EditorBrowsableState.Never)]
public static class TypeContextExtensions
{
    public static void Execute(this ITypeContext typeContext, ITypeAction action)
    {
        ThrowHelper.ThrowIfNull(typeContext);
        ThrowHelper.ThrowIfNull(action);
        
        typeContext.Execute(ref action);
    }
    
    public static TReturn Execute<TReturn>(this ITypeContext typeContext, ITypeAction<TReturn> action)
    {
        ThrowHelper.ThrowIfNull(typeContext);
        ThrowHelper.ThrowIfNull(action);
        
        return typeContext.Execute<ITypeAction<TReturn>, TReturn>(ref action);
    }
    
    public static void ExecuteGeneric(this ITypeContext typeContext, IGenericTypeAction action)
    {
        ThrowHelper.ThrowIfNull(typeContext);
        ThrowHelper.ThrowIfNull(action);
        
        typeContext.ExecuteGeneric(ref action);
    }
    
    public static TReturn ExecuteGeneric<TReturn>(this ITypeContext typeContext, IGenericTypeAction<TReturn> action)
    {
        ThrowHelper.ThrowIfNull(typeContext);
        ThrowHelper.ThrowIfNull(action);
        
        return typeContext.ExecuteGeneric<IGenericTypeAction<TReturn>, TReturn>(ref action);
    }
}