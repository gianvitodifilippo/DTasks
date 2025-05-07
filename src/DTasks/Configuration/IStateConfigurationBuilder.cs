using DTasks.Infrastructure.State;

namespace DTasks.Configuration;

public interface IStateConfigurationBuilder
{
    IStateConfigurationBuilder UseStack(FlowComponentFactory<IDAsyncStack> factory);

    IStateConfigurationBuilder UseHeap(RootComponentFactory<IDAsyncHeap> factory);
}
