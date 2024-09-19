using System.Diagnostics.CodeAnalysis;
using DTasks.Extensions.Microsoft.DependencyInjection.Mapping;

namespace DTasks.Extensions.Microsoft.DependencyInjection.Hosting;

internal sealed class ChildDTaskScope(IServiceProvider provider, IServiceRegister register, RootDTaskScope root) : DTaskScope(provider, register), IChildServiceMapper
{
    public override bool TryGetReferenceToken(object reference, [NotNullWhen(true)] out object? token)
    {
        return
            base.TryGetReferenceToken(reference, out token) ||
            root.TryGetReferenceToken(reference, out token);
    }

    void IChildServiceMapper.MapService(object service, ServiceToken token) => MapService(service, token);
}
