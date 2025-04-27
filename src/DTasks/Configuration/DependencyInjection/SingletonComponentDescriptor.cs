using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Configuration.DependencyInjection;

[DebuggerDisplay("{ToString(),nq}")]
internal sealed class SingletonComponentDescriptor<TComponent>(ConfiguredImplementationFactory<TComponent> createComponent) : IComponentDescriptor<TComponent>
    where TComponent : notnull
{
    public TReturn Build<TReturn>(IDAsyncInfrastructureBuilder<TComponent, TReturn> builder)
    {
        return builder.Singleton(createComponent);
    }

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"Singleton({typeof(TComponent).Name}))";
    }
}