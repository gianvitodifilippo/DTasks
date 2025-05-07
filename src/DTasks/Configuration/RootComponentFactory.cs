namespace DTasks.Configuration;

public abstract class RootComponentFactory<TComponent>
{
    protected abstract TComponent CreateComponentCore(IDAsyncRootScope rootScope);

    public TComponent CreateComponent(IDAsyncRootScope rootScope)
    {
        return CreateComponentCore(rootScope);
    }

    public static implicit operator RootComponentFactory<TComponent>(TComponent component) => new ConstantRootComponentFactory(component);

    public static implicit operator RootComponentFactory<TComponent>(Func<TComponent> factory) => new Delegate0RootComponentFactory(factory);
    
    public static implicit operator RootComponentFactory<TComponent>(Func<IDAsyncRootScope, TComponent> factory) => new Delegate1RootComponentFactory(factory);

    private sealed class ConstantRootComponentFactory(TComponent component) : RootComponentFactory<TComponent>
    {
        protected override TComponent CreateComponentCore(IDAsyncRootScope rootScope)
        {
            return component;
        }
    }

    private sealed class Delegate0RootComponentFactory(Func<TComponent> factory) : RootComponentFactory<TComponent>
    {
        protected override TComponent CreateComponentCore(IDAsyncRootScope rootScope)
        {
            return factory();
        }
    }

    private sealed class Delegate1RootComponentFactory(Func<IDAsyncRootScope, TComponent> factory) : RootComponentFactory<TComponent>
    {
        protected override TComponent CreateComponentCore(IDAsyncRootScope rootScope)
        {
            return factory(rootScope);
        }
    }
}

