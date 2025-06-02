using System.ComponentModel;

namespace DTasks.Infrastructure.Generics;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ITypeContext
{
    Type Type { get; }
    
    Type GenericType { get; }
    
    bool IsGeneric { get; }
    
    void Execute<TAction>(ref TAction action)
        where TAction : ITypeAction;
    
    TReturn Execute<TAction, TReturn>(ref TAction action)
        where TAction : ITypeAction<TReturn>;
    
    void ExecuteGeneric<TAction>(ref TAction action)
        where TAction : IGenericTypeAction;
    
    TReturn ExecuteGeneric<TAction, TReturn>(ref TAction action)
        where TAction : IGenericTypeAction<TReturn>;
}
