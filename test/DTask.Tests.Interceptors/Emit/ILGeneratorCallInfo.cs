using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DTask.Tests.Interceptors.Emit;

internal readonly record struct ILGeneratorCallInfo(InvocationExpressionSyntax Node, InterceptedMethodInfo MethodInfo)
{
    public static bool IsNotDefault(ILGeneratorCallInfo info) => info.Node is not null;
}
