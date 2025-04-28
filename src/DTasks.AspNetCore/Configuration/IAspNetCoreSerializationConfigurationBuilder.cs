using DTasks.Configuration.DependencyInjection;
using DTasks.Serialization;
using DTasks.Serialization.Configuration;
using DTasks.Serialization.Json.Configuration;

namespace DTasks.AspNetCore.Configuration;

public interface IAspNetCoreSerializationConfigurationBuilder : ISerializationConfigurationBuilder
{
    new IAspNetCoreSerializationConfigurationBuilder UseStateMachineSerializer(IComponentDescriptor<IStateMachineSerializer> descriptor);

    new IAspNetCoreSerializationConfigurationBuilder UseSerializer(IComponentDescriptor<IDAsyncSerializer> descriptor);

    new IAspNetCoreSerializationConfigurationBuilder UseStorage(IComponentDescriptor<IDAsyncStorage> descriptor);

    IAspNetCoreSerializationConfigurationBuilder ConfigureJsonFormat(Action<IJsonFormatConfigurationBuilder> configure);
}
