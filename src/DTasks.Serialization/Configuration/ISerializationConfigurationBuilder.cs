using DTasks.Configuration.DependencyInjection;
using DTasks.Serialization;

namespace DTasks.Configuration;

public interface ISerializationConfigurationBuilder
{
    ISerializationConfigurationBuilder UseStateMachineSerializer(IComponentDescriptor<IStateMachineSerializer> descriptor);

    ISerializationConfigurationBuilder UseSerializer(IComponentDescriptor<IDAsyncSerializer> descriptor);

    ISerializationConfigurationBuilder UseStorage(IComponentDescriptor<IDAsyncStorage> descriptor);
}
