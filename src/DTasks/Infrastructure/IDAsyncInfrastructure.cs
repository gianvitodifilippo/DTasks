using DTasks.Configuration.DependencyInjection;
using DTasks.Infrastructure.DependencyInjection;
using DTasks.Infrastructure.Execution;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;

namespace DTasks.Infrastructure;

internal interface IDAsyncInfrastructure
{
    IDAsyncRootInfrastructure RootInfrastructure { get; }
    
    RootComponentProvider RootProvider { get; }
    
    IDAsyncRootScope RootScope { get; } // TODO: Remove and pass it as separate field to RootComponentProvider
    
    IDAsyncTypeResolver TypeResolver { get; }
    
    IDAsyncHeap GetHeap(IComponentProvider hostProvider);
    
    IDAsyncStack GetStack(IComponentProvider flowProvider);
    
    IDAsyncSurrogator GetSurrogator(IComponentProvider flowProvider);
    
    IDAsyncCancellationProvider GetCancellationProvider(IComponentProvider flowProvider);
    
    IDAsyncSuspensionHandler GetSuspensionHandler(IComponentProvider flowProvider);
}
