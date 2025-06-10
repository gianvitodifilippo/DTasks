using System.ComponentModel;

namespace DTasks.Infrastructure.Generics;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class TypeContext
{
    public static readonly ITypeContext Void = new VoidTypeContext();
    
    public static ITypeContext Of<T>() => new TypeContext<T>(isStateMachine: false);
    
    public static ITypeContext StateMachine<T>() => new TypeContext<T>(isStateMachine: true);
    
    private sealed class VoidTypeContext : NonGenericTypeContext
    {
        public override Type Type => typeof(void);

        public override bool IsStateMachine => false;

        public override void Execute<TAction>(scoped ref TAction action) => action.Invoke<object>();

        public override TReturn Execute<TAction, TReturn>(scoped ref TAction action) => action.Invoke<object>();
    }
}

internal sealed class TypeContext<T>(bool isStateMachine) : NonGenericTypeContext
{
    public override Type Type => typeof(T);

    public override bool IsStateMachine => isStateMachine;

    public override void Execute<TAction>(scoped ref TAction action) => action.Invoke<T>();

    public override TReturn Execute<TAction, TReturn>(scoped ref TAction action) => action.Invoke<T>();
}