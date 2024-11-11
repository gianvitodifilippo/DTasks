using System.ComponentModel;

namespace DTasks.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ITokenConverter
{
    T Convert<TToken, T>(TToken token);
}
