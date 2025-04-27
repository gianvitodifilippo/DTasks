using System.Text.Json;
using DTasks.Configuration.DependencyInjection;
using DTasks.Inspection;

namespace DTasks.Serialization.Json.Configuration;

public interface IJsonFormatConfigurationBuilder
{
    IJsonFormatConfigurationBuilder ConfigureSerializerOptions(Action<JsonSerializerOptions> configure);

    IJsonFormatConfigurationBuilder UseInspector(IComponentDescriptor<IStateMachineInspector> descriptor);
}
