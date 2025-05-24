using System.ComponentModel;

namespace DTasks.Infrastructure.Generics;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ITypeContext
{
    Type Type { get; }
    
    void Execute<TAction>(ref TAction action)
        where TAction : ITypeAction;
    
    TReturn Execute<TAction, TReturn>(ref TAction action)
        where TAction : ITypeAction<TReturn>;
}
