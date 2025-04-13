using DTasks.Infrastructure.Marshaling;
using DTasks.Utils;

namespace DTasks.Infrastructure;

public sealed partial class DAsyncFlow : ISuspensionContext
{
    IDAsyncSurrogator ISuspensionContext.Surrogator => this;

    bool ISuspensionContext.IsSuspended<TAwaiter>(ref TAwaiter awaiter)
    {
        Assert.NotNull(_suspendingAwaiterOrType);

        return typeof(TAwaiter).IsValueType
            ? _suspendingAwaiterOrType is Type type && type == typeof(TAwaiter)
            : ReferenceEquals(_suspendingAwaiterOrType, awaiter);
    }
}
