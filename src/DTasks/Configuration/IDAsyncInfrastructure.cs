using DTasks.Infrastructure;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Configuration;

internal interface IDAsyncInfrastructure
{
    IDAsyncStack GetStack(IDAsyncScope scope);
    
    IDAsyncHeap GetHeap(IDAsyncScope scope);
    
    IDAsyncSurrogator GetSurrogator(IDAsyncScope scope);
    
    IDAsyncCancellationProvider GetCancellationProvider(IDAsyncScope scope);
    
    IDAsyncSuspensionHandler GetSuspensionHandler(IDAsyncScope scope);
}