using DTasks.Configuration.DependencyInjection;
using DTasks.Infrastructure;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Configuration;

internal sealed class DAsyncInfrastructureBuilder
{
    private readonly List<IComponentDescriptor<IDAsyncSurrogator>> _surrogatorDescriptors = [];
    private IComponentDescriptor<IDAsyncStack>? _stackDescriptor;
    private IComponentDescriptor<IDAsyncHeap>? _heapDescriptor;
    private IComponentDescriptor<IDAsyncCancellationProvider>? _cancellationProviderDescriptor;
    private IComponentDescriptor<IDAsyncSuspensionHandler>? _suspensionHandlerDescriptor;

    public IDAsyncInfrastructure BuildInfrastructure(DTasksConfiguration configuration)
    {
        return new DAsyncInfrastructure(this, configuration);
    }

    private IComponentDescriptor<IDAsyncStack> StackDescriptor => EnsureAssigned(_stackDescriptor);

    private IComponentDescriptor<IDAsyncHeap> HeapDescriptor => EnsureAssigned(_heapDescriptor);

    private IComponentDescriptor<IDAsyncSurrogator> SurrogatorDescriptor => _surrogatorDescriptors.Count switch
    {
        0 => ComponentDescriptor.Singleton(DAsyncSurrogator.Default),
        1 => _surrogatorDescriptors[0],
        _ => ComponentDescriptor.Aggregate(_surrogatorDescriptors, DAsyncSurrogator.Aggregate)
    };

    private IComponentDescriptor<IDAsyncCancellationProvider> CancellationProviderDescriptor => OrDefault(_cancellationProviderDescriptor, DAsyncCancellationProvider.Default);

    private IComponentDescriptor<IDAsyncSuspensionHandler> SuspensionHandlerProvider => OrDefault(_suspensionHandlerDescriptor, DAsyncSuspensionHandler.Default);
    public void UseStack(IComponentDescriptor<IDAsyncStack> descriptor)
    {
        _stackDescriptor = descriptor;
    }

    public void UseHeap(IComponentDescriptor<IDAsyncHeap> descriptor)
    {
        _heapDescriptor = descriptor;
    }

    public void AddSurrogator(IComponentDescriptor<IDAsyncSurrogator> descriptor)
    {
        _surrogatorDescriptors.Add(descriptor);
    }

    public void UseCancellationProvider(IComponentDescriptor<IDAsyncCancellationProvider> descriptor)
    {
        _cancellationProviderDescriptor = descriptor;
    }

    public void UseSuspensionHandler(IComponentDescriptor<IDAsyncSuspensionHandler> descriptor)
    {
        _suspensionHandlerDescriptor = descriptor;
    }

    private static IComponentDescriptor<TComponent> EnsureAssigned<TComponent>(IComponentDescriptor<TComponent>? descriptor)
        where TComponent : notnull
    {
        return descriptor ?? throw new InvalidOperationException($"Missing required component of type {typeof(TComponent).Name})");
    }

    private static IComponentDescriptor<TComponent> OrDefault<TComponent>(IComponentDescriptor<TComponent>? descriptor, TComponent defaultComponent)
        where TComponent : notnull
    {
        return descriptor ?? ComponentDescriptor.Singleton(defaultComponent);
    }

    private sealed class DAsyncInfrastructure(
        DAsyncInfrastructureBuilder builder,
        DTasksConfiguration configuration) : IDAsyncInfrastructure
    {
        private readonly IComponentProvider<IDAsyncStack> _stackProvider = ComponentProviderFactory.CreateProvider(configuration, builder.StackDescriptor);
        private readonly IComponentProvider<IDAsyncHeap> _heapProvider = ComponentProviderFactory.CreateProvider(configuration, builder.HeapDescriptor);
        private readonly IComponentProvider<IDAsyncSurrogator> _surrogatorProvider = ComponentProviderFactory.CreateProvider(configuration, builder.SurrogatorDescriptor);
        private readonly IComponentProvider<IDAsyncCancellationProvider> _cancellationProviderProvider = ComponentProviderFactory.CreateProvider(configuration, builder.CancellationProviderDescriptor);
        private readonly IComponentProvider<IDAsyncSuspensionHandler> _suspensionHandlerProvider = ComponentProviderFactory.CreateProvider(configuration, builder.SuspensionHandlerProvider);

        public IDAsyncStack GetStack(IDAsyncScope scope) => _stackProvider.GetComponent(scope);

        public IDAsyncHeap GetHeap(IDAsyncScope scope) => _heapProvider.GetComponent(scope);

        public IDAsyncSurrogator GetSurrogator(IDAsyncScope scope) => _surrogatorProvider.GetComponent(scope);

        public IDAsyncCancellationProvider GetCancellationProvider(IDAsyncScope scope) => _cancellationProviderProvider.GetComponent(scope);

        public IDAsyncSuspensionHandler GetSuspensionHandler(IDAsyncScope scope) => _suspensionHandlerProvider.GetComponent(scope);
    }
}