using DTasks.Execution;

namespace DTasks.Infrastructure.Features;

public interface IDAsyncSuspensionFeature
{
    void Suspend(ISuspensionCallback callback);
}
