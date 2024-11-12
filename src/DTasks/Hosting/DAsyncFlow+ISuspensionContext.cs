using DTasks.Utils;

namespace DTasks.Hosting;

internal partial class DAsyncFlow : ISuspensionContext
{
    bool ISuspensionContext.IsSuspended<TAwaiter>(ref TAwaiter awaiter)
    {
        Assert.NotNull(_suspendingAwaiterOrType);

        return typeof(TAwaiter).IsValueType
            ? _suspendingAwaiterOrType is Type type && type == typeof(TAwaiter)
            : ReferenceEquals(_suspendingAwaiterOrType, awaiter);
    }
}
