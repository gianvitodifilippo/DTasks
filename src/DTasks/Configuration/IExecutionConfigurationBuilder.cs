using DTasks.Infrastructure.Execution;

namespace DTasks.Configuration;

public interface IExecutionConfigurationBuilder
{
    IExecutionConfigurationBuilder UseCancellationProvider(RootComponentFactory<IDAsyncCancellationProvider> factory);

    IExecutionConfigurationBuilder UseSuspensionHandler(RootComponentFactory<IDAsyncSuspensionHandler> factory);
}
