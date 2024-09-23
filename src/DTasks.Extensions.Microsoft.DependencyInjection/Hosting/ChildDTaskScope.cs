using DTasks.Extensions.Microsoft.DependencyInjection.Mapping;
using System.Diagnostics.CodeAnalysis;

namespace DTasks.Extensions.Microsoft.DependencyInjection.Hosting;

internal sealed class ChildDTaskScope(IServiceProvider provider, IServiceRegister register, RootDTaskScope root) : DTaskScope(provider, register), IChildServiceMapper
{
    public override bool TryGetReferenceToken(object reference, [NotNullWhen(true)] out object? token)
    {
        return
            base.TryGetReferenceToken(reference, out token) ||
            root.TryGetReferenceToken(reference, out token);
    }

    public new void MapService(object service, ServiceToken token) => base.MapService(service, token);
}
