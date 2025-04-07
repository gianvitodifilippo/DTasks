#if !DEBUG_TESTS && !RELEASE_TESTS

using System.Reflection.Emit;
using Xunit.Sdk;

namespace DTasks.Inspection.Dynamic;

public static class ILGeneratorInterceptors
{
    public static IDisposable InterceptCalls(this ILGenerator il)
    {
        throw FailException.ForFailure($"Switch your configuration to either Debug-Tests or Release-Tests to use '{nameof(InterceptCalls)}'.");
    }
}

#endif
