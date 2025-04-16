using DTasks.Infrastructure;
using DTasks.Infrastructure.Marshaling;
using System.Diagnostics;

namespace DTasks.AspNetCore.Infrastructure;

// TODO: Internal
public sealed class DAsyncFlowServices : IDAsyncFlowServices
{
    private IDAsyncSurrogator? _surrogator;

    public IDAsyncSurrogator Surrogator => _surrogator ?? throw new InvalidOperationException("A d-async flow was not initialized.");

    public Scope UseFlowServices(IDAsyncHostCreationContext context)
    {
        Debug.Assert(_surrogator is null);

        _surrogator = context.Surrogator;
        return new Scope(this);
    }

    public readonly struct Scope(DAsyncFlowServices services) : IDisposable
    {
        public void Dispose()
        {
            Debug.Assert(services._surrogator is not null);

            services._surrogator = null;
        }
    }
}
