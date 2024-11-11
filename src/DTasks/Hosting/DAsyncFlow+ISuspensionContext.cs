using System.Diagnostics;

namespace DTasks.Hosting;

internal partial class DAsyncFlow : ISuspensionContext
{
    bool ISuspensionContext.IsSuspended<TAwaiter>(ref TAwaiter awaiter)
    {
        Debug.Assert(_suspendingAwaiterOrType is not null);

        return typeof(TAwaiter).IsValueType
            ? _suspendingAwaiterOrType is Type type && type == typeof(TAwaiter)
            : ReferenceEquals(_suspendingAwaiterOrType, awaiter);
    }
}
