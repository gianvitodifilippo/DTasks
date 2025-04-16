using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ISurrogationAction
{
    void SurrogateAs<TSurrogate>(TypeId typeId, TSurrogate surrogate);
}
