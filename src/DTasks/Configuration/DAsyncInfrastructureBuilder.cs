using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Configuration;

internal sealed class DAsyncInfrastructureBuilder
{
    private readonly List<RootComponentFactory<IDAsyncSurrogator>> _surrogatorFactories = [];
    private FlowComponentFactory<IDAsyncStack>? _stackFactory;
    private RootComponentFactory<IDAsyncHeap>? _heapFactory;
    private RootComponentFactory<IDAsyncCancellationProvider>? _cancellationProviderFactory;
    private RootComponentFactory<IDAsyncSuspensionHandler>? _suspensionHandlerFactory;

    public IDAsyncInfrastructure Build(IDAsyncRootScope rootScope)
    {
        return new DAsyncInfrastructure(this, rootScope);
    }

    public void UseStack(FlowComponentFactory<IDAsyncStack> factory)
    {
        _stackFactory = factory;
    }

    public void UseHeap(RootComponentFactory<IDAsyncHeap> factory)
    {
        _heapFactory = factory;
    }

    public void AddSurrogator(RootComponentFactory<IDAsyncSurrogator> factory)
    {
        _surrogatorFactories.Add(factory);
    }

    public void UseCancellationProvider(RootComponentFactory<IDAsyncCancellationProvider> factory)
    {
        _cancellationProviderFactory = factory;
    }

    public void UseSuspensionHandler(RootComponentFactory<IDAsyncSuspensionHandler> factory)
    {
        _suspensionHandlerFactory = factory;
    }

    private Func<IDAsyncFlowScope, IDAsyncStack> CreateStackFactory(IDAsyncRootScope rootScope)
    {
        if (_stackFactory is null)
            throw MissingRequiredComponent(nameof(IDAsyncStack));

        return _stackFactory.GetScopedFactory(rootScope);
    }

    private IDAsyncHeap CreateHeap(IDAsyncRootScope rootScope)
    {
        if (_heapFactory is null)
            throw MissingRequiredComponent(nameof(IDAsyncHeap));

        return _heapFactory.CreateComponent(rootScope);
    }

    private IDAsyncSurrogator CreateSurrogator(IDAsyncRootScope rootScope)
    {
        return _surrogatorFactories.Count switch
        {
            0 => DAsyncSurrogator.Default,
            1 => _surrogatorFactories[0].CreateComponent(rootScope),
            _ => DAsyncSurrogator.Aggregate(_surrogatorFactories.Select(factory => factory.CreateComponent(rootScope)))
        };
    }

    private IDAsyncCancellationProvider CreateCancellationProvider(IDAsyncRootScope rootScope)
    {
        if (_cancellationProviderFactory is null)
            return DAsyncCancellationProvider.Default;

        return _cancellationProviderFactory.CreateComponent(rootScope);
    }

    private IDAsyncSuspensionHandler CreateSuspensionHandler(IDAsyncRootScope rootScope)
    {
        if (_suspensionHandlerFactory is null)
            return DAsyncSuspensionHandler.Default;

        return _suspensionHandlerFactory.CreateComponent(rootScope);
    }

    private static InvalidOperationException MissingRequiredComponent(string componentName)
    {
        return new InvalidOperationException($"Required component of type '{componentName}' was not configured.");
    }

    private sealed class DAsyncInfrastructure : IDAsyncInfrastructure
    {
        private readonly Func<IDAsyncFlowScope, IDAsyncStack> _stackFactory;

        public DAsyncInfrastructure(DAsyncInfrastructureBuilder builder, IDAsyncRootScope rootScope)
        {
            _stackFactory = builder.CreateStackFactory(rootScope);
            TypeResolver = rootScope.TypeResolver;
            Heap = builder.CreateHeap(rootScope);
            Surrogator = builder.CreateSurrogator(rootScope);
            CancellationProvider = builder.CreateCancellationProvider(rootScope);
            SuspensionHandler = builder.CreateSuspensionHandler(rootScope);
        }

        public IDAsyncTypeResolver TypeResolver { get; }

        public IDAsyncHeap Heap { get; }

        public IDAsyncSurrogator Surrogator { get; }

        public IDAsyncCancellationProvider CancellationProvider { get; }

        public IDAsyncSuspensionHandler SuspensionHandler { get; }

        public IDAsyncStack GetStack(IDAsyncFlowScope scope) => _stackFactory(scope);
    }
}