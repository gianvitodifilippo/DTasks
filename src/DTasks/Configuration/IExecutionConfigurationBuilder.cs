using DTasks.Configuration.DependencyInjection;
using DTasks.Infrastructure.Execution;

namespace DTasks.Configuration;

public interface IExecutionConfigurationBuilder
{
    IExecutionConfigurationBuilder UseCancellationProvider(IComponentDescriptor<IDAsyncCancellationProvider> descriptor);
    
    IExecutionConfigurationBuilder UseSuspensionHandler(IComponentDescriptor<IDAsyncSuspensionHandler> descriptor);
}
