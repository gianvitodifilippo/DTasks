using DTasks.Configuration.DependencyInjection;

namespace DTasks.Serialization.Configuration;

public interface ISerializationConfigurationBuilder
{
    ISerializationConfigurationBuilder UseStateMachineSerializer(IComponentDescriptor<IStateMachineSerializer> descriptor);

    ISerializationConfigurationBuilder UseSerializer(IComponentDescriptor<IDAsyncSerializer> descriptor);

    ISerializationConfigurationBuilder UseStorage(IComponentDescriptor<IDAsyncStorage> descriptor);
}
