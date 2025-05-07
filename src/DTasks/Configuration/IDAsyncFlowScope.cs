using System.ComponentModel;
using DTasks.Infrastructure.Marshaling;

namespace DTasks.Configuration;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IDAsyncFlowScope
{
    IDAsyncSurrogator Surrogator { get; }
}
