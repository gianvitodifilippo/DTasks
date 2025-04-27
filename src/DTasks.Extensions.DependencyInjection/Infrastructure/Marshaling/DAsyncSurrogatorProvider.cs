using DTasks.Infrastructure.Marshaling;
using Microsoft.Extensions.DependencyInjection;

namespace DTasks.Extensions.DependencyInjection.Infrastructure.Marshaling;

internal sealed class DAsyncSurrogatorProvider(IServiceProvider rootProvider)
{
    public IDAsyncSurrogator GetSurrogator(IServiceProvider provider)
    {
        return ReferenceEquals(provider, rootProvider)
            ? provider.GetRequiredService<ChildDAsyncSurrogator>()
            : provider.GetRequiredService<RootDAsyncSurrogator>();
    }
}