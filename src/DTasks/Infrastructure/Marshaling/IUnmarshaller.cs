using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IUnmarshaller
{
    TSurrogate ReadSurrogate<TSurrogate>(Type surrogateType);

    void BeginArray();

    void EndArray();
    
    T ReadItem<T>();
}