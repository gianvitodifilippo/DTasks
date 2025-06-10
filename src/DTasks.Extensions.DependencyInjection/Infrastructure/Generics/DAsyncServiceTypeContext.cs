using DTasks.Infrastructure.Generics;

namespace DTasks.Extensions.DependencyInjection.Infrastructure.Generics;

internal sealed class DAsyncServiceTypeContext(Type type) : NonGenericTypeContext
{
    public override Type Type => type;

    public override bool IsStateMachine => false;

    public override void Execute<TAction>(scoped ref TAction action) => action.Invoke<object>();

    public override TReturn Execute<TAction, TReturn>(scoped ref TAction action) => action.Invoke<object>();
}
