using System.ComponentModel;

namespace DTasks.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMarshalingAction
{
    void MarshalAs<TToken>(TypeId typeId, TToken token);
}
