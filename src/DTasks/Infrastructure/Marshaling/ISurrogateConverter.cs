using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ISurrogateConverter
{
    T Convert<TSurrogate, T>(TSurrogate surrogate);
}
