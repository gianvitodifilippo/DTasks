using System.Collections.Immutable;
using DTasks.Configuration.DependencyInjection;

namespace DTasks.Infrastructure.DependencyInjection;

internal sealed class AggregateComponentToken<TComponent>(
    ImmutableArray<IComponentToken<TComponent>> tokens,
    Func<ImmutableArray<TComponent>, TComponent> aggregate) : IInfrastructureComponentToken<TComponent>
{
    public TComponent CreateComponent(ComponentProvider provider)
    {
        var builder = ImmutableArray.CreateBuilder<TComponent>(tokens.Length);
        foreach (IComponentToken<TComponent> token in tokens)
        {
            builder.Add(provider.GetComponent(token));
        }
        
        return aggregate(builder.ToImmutable());
    }
}