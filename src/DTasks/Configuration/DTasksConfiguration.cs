using System.Collections.Immutable;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Configuration;

public sealed class DTasksConfiguration
{
    private DTasksConfiguration(DTasksConfigurationBuilder builder)
    {
        TypeResolver = builder.TypeResolver;
        SurrogatableTypes = builder.SurrogatableTypes;

        // BuildInfrastructure may use public properties of this instance,
        // therefore it must be call after those properties are assigned
        Infrastructure = builder.BuildInfrastructure(this);
    }

    public IDAsyncTypeResolver TypeResolver { get; }

    public ImmutableArray<Type> SurrogatableTypes { get; }

    internal IDAsyncInfrastructure Infrastructure { get; }

    public static DTasksConfiguration Create()
    {
        throw new NotImplementedException();
    }

    public static DTasksConfiguration Create(Action<IDTasksConfigurationBuilder> configure)
    {
        DTasksConfigurationBuilder builder = new();
        configure(builder);

        return new(builder);
    }
}
