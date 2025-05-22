using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling.Configuration;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IInfrastructureMarshalingBuilder
{
    IInfrastructureMarshalingBuilder SurrogateDTaskOf<T>();
    
    IInfrastructureMarshalingBuilder AwaitWhenAllOf<T>();
}