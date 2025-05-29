using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DTasks.Utils;

public static class Reinterpret
{
    public static TTo Cast<TTo>([NotNull] object? value)
        where TTo : class
    {
        Assert.Is<TTo>(value);
        return Unsafe.As<TTo>(value);
    }
}