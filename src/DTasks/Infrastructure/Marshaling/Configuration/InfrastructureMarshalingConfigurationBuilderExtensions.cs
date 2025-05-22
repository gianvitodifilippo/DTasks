using System.ComponentModel;
using DTasks.Infrastructure.Marshaling.Configuration;

namespace DTasks.Configuration;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class InfrastructureMarshalingConfigurationBuilderExtensions
{
    public static IMarshalingConfigurationBuilder ConfigureInfrastructure(this IMarshalingConfigurationBuilder builder, Action<IInfrastructureMarshalingBuilder> configure)
    {
        InfrastructureMarshalingBuilder infrastructureBuilder = new InfrastructureMarshalingBuilder(builder);
        configure(infrastructureBuilder);

        return builder;
    }
}
