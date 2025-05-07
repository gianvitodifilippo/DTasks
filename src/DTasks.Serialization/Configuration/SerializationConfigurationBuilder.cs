using DTasks.Configuration;
using DTasks.Infrastructure.State;

namespace DTasks.Serialization.Configuration;

internal sealed class SerializationConfigurationBuilder : ISerializationConfigurationBuilder
{
    private FlowComponentFactory<IStateMachineSerializer>? _stateMachineSerializerFactory;
    private RootComponentFactory<IDAsyncSerializer>? _serializerFactory;
    private RootComponentFactory<IDAsyncStorage> _storageFactory = DAsyncStorage.Default;

    public TBuilder Configure<TBuilder>(TBuilder builder)
        where TBuilder : IDTasksConfigurationBuilder
    {
        if (_stateMachineSerializerFactory is null || _serializerFactory is null)
            throw new InvalidOperationException("Serialization was not properly configured."); // TODO: Dedicated exception type

        FlowComponentFactory<IDAsyncStack> stackFactory = delegate (IDAsyncRootScope rootScope, IDAsyncFlowScope flowScope)
        {
            IStateMachineSerializer stateMachineSerializer = _stateMachineSerializerFactory.CreateComponent(rootScope, flowScope);
            IDAsyncStorage 

            new BinaryDAsyncStack(stateMachineSerializer, storage);
        };

        IComponentFactory<IDAsyncHeap> heapFactory = ComponentFactory.Combine(
            _serializerFactory,
            _storageFactory,
            (serializer, storage) => new BinaryDAsyncHeap(serializer, storage));

        builder.ConfigureState(stateManager => stateManager
            .UseStack(stackFactory)
            .UseHeap(heapFactory));

        return builder;
    }

    ISerializationConfigurationBuilder ISerializationConfigurationBuilder.UseStateMachineSerializer(IComponentFactory<IStateMachineSerializer> descriptor)
    {
        _stateMachineSerializerFactory = descriptor;
        return this;
    }

    ISerializationConfigurationBuilder ISerializationConfigurationBuilder.UseSerializer(IComponentFactory<IDAsyncSerializer> descriptor)
    {
        _serializerFactory = descriptor;
        return this;
    }

    ISerializationConfigurationBuilder ISerializationConfigurationBuilder.UseStorage(IComponentFactory<IDAsyncStorage> descriptor)
    {
        _storageFactory = descriptor;
        return this;
    }
}