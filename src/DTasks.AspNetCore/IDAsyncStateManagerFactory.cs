using DTasks.Infrastructure;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.AspNetCore;

internal interface IDAsyncStateManagerFactory
{
    IDAsyncStateManager CreateStateManager(IDAsyncMarshaler marshaler);
}