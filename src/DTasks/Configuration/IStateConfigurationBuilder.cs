using DTasks.Configuration.DependencyInjection;
using DTasks.Infrastructure.State;

namespace DTasks.Configuration;

public interface IStateConfigurationBuilder
{
    IStateConfigurationBuilder UseStack(IComponentDescriptor<IDAsyncStack> descriptor);

    IStateConfigurationBuilder UseHeap(IComponentDescriptor<IDAsyncHeap> descriptor);
}
