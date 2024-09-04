using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace DTask.Tests.Interceptors.Emit;

[Generator]
public class EmitInterceptorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var values = context.SyntaxProvider
            .CreateSyntaxProvider(OfGetILGeneratorInvocation, ToInterceptionLocationInfo)
            .Where(static info => info != default)
            .Collect();

        context.RegisterImplementationSourceOutput(values, (context, locations) =>
        {
            if (locations.IsEmpty)
                return;

            var stringBuilder = new StringBuilder();
            var renderer = new ILGeneratorInterceptorsRenderer(stringBuilder);
            renderer.Render(locations);

            context.AddSource("ILGeneratorInterceptors", stringBuilder.ToString());
        });
    }

    private static bool OfGetILGeneratorInvocation(SyntaxNode node, CancellationToken cancellationToken) => node is InvocationExpressionSyntax
    {
        Expression: MemberAccessExpressionSyntax
        {
            Name.Identifier.ValueText: "GetILGenerator"
        }
    };

    private static InterceptionLocationInfo ToInterceptionLocationInfo(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var node = (InvocationExpressionSyntax)context.Node;
        IOperation? operation = context.SemanticModel.GetOperation(node, cancellationToken);

        if (operation is not IInvocationOperation { Instance.Type.Name: "DynamicMethod", Arguments.Length: 0 } invocation)
            return default;

        LinePosition position = ((MemberAccessExpressionSyntax)node.Expression).Name.GetLocation().GetLineSpan().StartLinePosition;
        return new InterceptionLocationInfo(node.SyntaxTree.FilePath, position.Line + 1, position.Character + 1);
    }
}
