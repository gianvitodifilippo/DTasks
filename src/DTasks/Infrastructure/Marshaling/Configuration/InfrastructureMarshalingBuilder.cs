using DTasks.Configuration;

namespace DTasks.Infrastructure.Marshaling.Configuration;

internal sealed class InfrastructureMarshalingBuilder(IMarshalingConfigurationBuilder builder) : IInfrastructureMarshalingBuilder
{
    public IInfrastructureMarshalingBuilder SurrogateDTaskOf<TResult>()
    {
        builder.RegisterSurrogatableType(DTaskTypeContext<TResult>.Instance);
        return this;
    }

    public IInfrastructureMarshalingBuilder AwaitWhenAllOf<TResult>()
    {
        DAsyncFlow.RegisterGenericTypeIds(builder, typeof(TResult));
        return this;
    }
}