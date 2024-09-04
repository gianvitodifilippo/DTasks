using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;
using System.Text;

namespace DTask.Tests.Interceptors.Emit;

[Generator]
public class EmitInterceptorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var values = context.SyntaxProvider
            .CreateSyntaxProvider(ForCallsToILGenerator, EmitCallsToILGenerator)
            .Where(ILGeneratorCallInfo.IsNotDefault)
            .Collect();

        context.RegisterImplementationSourceOutput(values, (context, callInfoList) =>
        {
            Dictionary<InterceptedMethodInfo, List<InvocationExpressionSyntax>> invocations = GroupInvocations(callInfoList);

            var stringBuilder = new StringBuilder();
            var renderer = new ILGeneratorInterceptorsRenderer(stringBuilder);
            renderer.Render(invocations);

            context.AddSource("ILGeneratorInterceptors", stringBuilder.ToString());
        });
    }

    private static Dictionary<InterceptedMethodInfo, List<InvocationExpressionSyntax>> GroupInvocations(ImmutableArray<ILGeneratorCallInfo> callInfoList)
    {
        Dictionary<InterceptedMethodInfo, List<InvocationExpressionSyntax>> groupedNodes = [];
        foreach (ILGeneratorCallInfo callInfo in callInfoList)
        {
            InterceptedMethodInfo methodInfo = callInfo.MethodInfo;
            if (!groupedNodes.TryGetValue(methodInfo, out List<InvocationExpressionSyntax>? nodes))
            {
                nodes = [];
                groupedNodes.Add(methodInfo, nodes);
            }

            nodes.Add(callInfo.Node);
        }

        return groupedNodes;
    }

    private static bool ForCallsToILGenerator(SyntaxNode node, CancellationToken cancellationToken) => node is InvocationExpressionSyntax
    {
        Expression: MemberAccessExpressionSyntax
        {
            Name.Identifier.ValueText: "Emit" or "DefineLabel" or "MarkLabel" or "DeclareLocal"
        }
    };

    private static ILGeneratorCallInfo EmitCallsToILGenerator(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var node = (InvocationExpressionSyntax)context.Node;

        IOperation? operation = context.SemanticModel.GetOperation(node, cancellationToken);

        if (operation is not IInvocationOperation { Instance.Type.Name: "ILGenerator", Arguments.Length: < 3 } invocation)
            return default;

        if (invocation.Arguments.Length == 0)
            return new(node, new InterceptedMethodInfo(InterceptedMethodKind.DefineLabel, ArgumentType.None));

        if (invocation.Arguments.Length == 1)
        {
            var methodInfo = invocation.TargetMethod.Name switch
            {
                "Emit"         => new InterceptedMethodInfo(InterceptedMethodKind.Emit, ArgumentType.None),
                "MarkLabel"    => new InterceptedMethodInfo(InterceptedMethodKind.MarkLabel, ArgumentType.Label),
                "DeclareLocal" => new InterceptedMethodInfo(InterceptedMethodKind.DeclareLocal, ArgumentType.Type),
                _              => throw new InvalidOperationException()
            };

            return new(node, methodInfo);
        }

        if (invocation.Arguments[1].Value.Type is not ITypeSymbol { Name: string argumentTypeName })
            return default;

        ArgumentType argumentType = argumentTypeName switch
        {
            "Byte"            => ArgumentType.Byte,
            "Int16"           => ArgumentType.Short,
            "Int32"           => ArgumentType.Int,
            "Int64"           => ArgumentType.Long,
            "Single"          => ArgumentType.Float,
            "Double"          => ArgumentType.Double,
            "String"          => ArgumentType.String,
            "Type"            => ArgumentType.Type,
            "MethodInfo"      => ArgumentType.MethodInfo,
            "ConstructorInfo" => ArgumentType.ConstructorInfo,
            "FieldInfo"       => ArgumentType.FieldInfo,
            "LocalBuilder"    => ArgumentType.LocalBuilder,
            "Label"           => ArgumentType.Label,
            ""                => ArgumentType.LabelArray, // The only array type there is on all overloads
            "SignatureHelper" => ArgumentType.SignatureHelper,
            _                 => ArgumentType.None
        };

        if (argumentType is ArgumentType.None)
            return default;

        return new(node, new InterceptedMethodInfo(InterceptedMethodKind.Emit, argumentType));
    }
}
