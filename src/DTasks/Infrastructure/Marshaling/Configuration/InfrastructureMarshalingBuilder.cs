using DTasks.Configuration;

namespace DTasks.Infrastructure.Marshaling.Configuration;

internal sealed class InfrastructureMarshalingBuilder(IMarshalingConfigurationBuilder builder) : IInfrastructureMarshalingBuilder
{
    public IInfrastructureMarshalingBuilder SurrogateDTaskOf<T>()
    {
        builder.RegisterSurrogatableType<DTask<T>>();
        return this;
    }

    public IInfrastructureMarshalingBuilder AwaitWhenAllOf<T>()
    {
        DAsyncFlow.RegisterGenericTypeIds(builder, typeof(T));
        return this;
    }
}