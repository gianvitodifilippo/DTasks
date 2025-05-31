using DTasks.Configuration;
using DTasks.Configuration.DependencyInjection;
using DTasks.Infrastructure;
using DTasks.Infrastructure.State;

namespace DTasks.Serialization.Configuration;

internal sealed class SerializationConfigurationBuilder : ISerializationConfigurationBuilder
{
    private IComponentDescriptor<IStateMachineSerializer>? _stateMachineSerializerDescriptor;
    private IComponentDescriptor<IDAsyncSerializer>? _serializerDescriptor;
    private IComponentDescriptor<IDAsyncStorage> _storageDescriptor = ComponentDescriptor.Singleton(DAsyncStorage.Default);

    public TBuilder Configure<TBuilder>(TBuilder builder)
        where TBuilder : IDTasksConfigurationBuilder
    {
        if (_stateMachineSerializerDescriptor is null || _serializerDescriptor is null)
            throw new InvalidOperationException("Serialization was not properly configured."); // TODO: Dedicated exception type, make message consistent

        IComponentDescriptor<IDAsyncStack> stackDescriptor =
            from stateMachineSerializer in _stateMachineSerializerDescriptor
            from storage in _storageDescriptor
            select new BinaryDAsyncStack(stateMachineSerializer, storage);
        
        IComponentDescriptor<IDAsyncHeap> heapDescriptor =
            from serializer in _serializerDescriptor
            from storage in _storageDescriptor
            select new BinaryDAsyncHeap(serializer, storage);
        
        builder.ConfigureState(stateManager => stateManager
            .UseStack(stackDescriptor)
            .UseHeap(heapDescriptor));

        return builder;
    }

    ISerializationConfigurationBuilder ISerializationConfigurationBuilder.UseStateMachineSerializer(IComponentDescriptor<IStateMachineSerializer> descriptor)
    {
        _stateMachineSerializerDescriptor = descriptor;
        return this;
    }

    ISerializationConfigurationBuilder ISerializationConfigurationBuilder.UseSerializer(IComponentDescriptor<IDAsyncSerializer> descriptor)
    {
        _serializerDescriptor = descriptor;
        return this;
    }

    ISerializationConfigurationBuilder ISerializationConfigurationBuilder.UseStorage(IComponentDescriptor<IDAsyncStorage> descriptor)
    {
        _storageDescriptor = descriptor;
        return this;
    }
}