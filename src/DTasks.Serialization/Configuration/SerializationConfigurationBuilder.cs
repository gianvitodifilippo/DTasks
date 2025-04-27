using DTasks.Configuration;
using DTasks.Configuration.DependencyInjection;
using DTasks.Infrastructure.State;

namespace DTasks.Serialization.Configuration;

internal sealed class SerializationConfigurationBuilder : ISerializationConfigurationBuilder
{
    private IComponentDescriptor<IStateMachineSerializer>? _stateMachineSerializerDescriptor;
    private IComponentDescriptor<IDAsyncSerializer>? _serializerDescriptor;
    private IComponentDescriptor<IDAsyncStorage>? _storageDescriptor;
    
    public ISerializationConfigurationBuilder UseStateMachineSerializer(IComponentDescriptor<IStateMachineSerializer> descriptor)
    {
        _stateMachineSerializerDescriptor = descriptor;
        return this;
    }

    public ISerializationConfigurationBuilder UseSerializer(IComponentDescriptor<IDAsyncSerializer> descriptor)
    {
        _serializerDescriptor = descriptor;
        return this;
    }

    public ISerializationConfigurationBuilder UseStorage(IComponentDescriptor<IDAsyncStorage> descriptor)
    {
        _storageDescriptor = descriptor;
        return this;
    }

    public TBuilder Configure<TBuilder>(TBuilder builder)
        where TBuilder : IDTasksConfigurationBuilder<TBuilder>
    {
        if (_stateMachineSerializerDescriptor is null || _serializerDescriptor is null || _storageDescriptor is null)
            throw new InvalidOperationException("Serialization was not properly configured."); // TODO: Dedicated exception type

        IComponentDescriptor<IDAsyncStack> stackDescriptor = ComponentDescriptor.Combine(
            _stateMachineSerializerDescriptor,
            _storageDescriptor,
            (stateMachineSerializer, storage) => new BinaryDAsyncStack(stateMachineSerializer, storage));
        
        IComponentDescriptor<IDAsyncHeap> heapDescriptor = ComponentDescriptor.Combine(
            _serializerDescriptor,
            _storageDescriptor,
            (serializer, storage) => new BinaryDAsyncHeap(serializer, storage));
        
        return builder.ConfigureState(stateManager => stateManager
            .UseStack(stackDescriptor)
            .UseHeap(heapDescriptor));
    }
}