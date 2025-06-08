using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMarshaller
{
    void WriteSurrogate<TSurrogate>(TypeId typeId, in TSurrogate value);

    void BeginArray(TypeId typeId, int memberCount);

    void EndArray();
    
    void WriteItem<T>(in T value);
}
