using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ISurrogatableTypeContext
{
    Type Type { get; }
    
    void Execute<TAction>(ref TAction action)
        where TAction : ISurrogatableTypeAction;
    
    TReturn Execute<TAction, TReturn>(ref TAction action)
        where TAction : ISurrogatableTypeAction<TReturn>;
}
