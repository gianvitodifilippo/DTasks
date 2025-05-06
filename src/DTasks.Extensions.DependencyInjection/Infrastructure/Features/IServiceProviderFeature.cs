using System.ComponentModel;

namespace DTasks.Extensions.DependencyInjection.Infrastructure.Features;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IServiceProviderFeature
{
    IServiceProvider Services { get; }
}