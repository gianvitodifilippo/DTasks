namespace DTasks.Configuration;

public abstract class FlowComponentFactory<TComponent>
{
    protected abstract TComponent CreateComponentCore(IDAsyncRootScope rootScope, IDAsyncFlowScope flowScope);

    public TComponent CreateComponent(IDAsyncRootScope rootScope, IDAsyncFlowScope flowScope)
    {
        return CreateComponentCore(rootScope, flowScope);
    }

    internal Func<IDAsyncFlowScope, TComponent> GetScopedFactory(IDAsyncRootScope rootScope)
    {
        return flowScope => CreateComponentCore(rootScope, flowScope);
    }

    public static implicit operator FlowComponentFactory<TComponent>(Func<IDAsyncFlowScope, TComponent> factory) => new Delegate1FlowComponentFactory(factory);

    public static implicit operator FlowComponentFactory<TComponent>(Func<IDAsyncRootScope, IDAsyncFlowScope, TComponent> factory) => new Delegate2FlowComponentFactory(factory);

    private sealed class Delegate1FlowComponentFactory(Func<IDAsyncFlowScope, TComponent> factory) : FlowComponentFactory<TComponent>
    {
        protected override TComponent CreateComponentCore(IDAsyncRootScope rootScope, IDAsyncFlowScope flowScope)
        {
            return factory(flowScope);
        }
    }

    private sealed class Delegate2FlowComponentFactory(Func<IDAsyncRootScope, IDAsyncFlowScope, TComponent> factory) : FlowComponentFactory<TComponent>
    {
        protected override TComponent CreateComponentCore(IDAsyncRootScope rootScope, IDAsyncFlowScope flowScope)
        {
            return factory(rootScope, flowScope);
        }
    }
}

