using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace DTasks.Tests.Interceptors.Emit;

[Generator]
public class EmitInterceptorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var methodBuilderInterceptionLocations = context.SyntaxProvider
            .CreateSyntaxProvider(OfGetILGeneratorInvocation, ToMethodBuilderInterceptionLocationInfo)
            .Where(static info => info != default)
            .Collect();

        var constructorBuilderInterceptionLocations = context.SyntaxProvider
            .CreateSyntaxProvider(OfGetILGeneratorInvocation, ToConstructorBuilderInterceptionLocationInfo)
            .Where(static info => info != default)
            .Collect();

        var parameters = methodBuilderInterceptionLocations
            .Combine(constructorBuilderInterceptionLocations)
            .Select((pair, cancellationToken) => new ILInterceptorParameters(pair.Left, pair.Right));

        context.RegisterImplementationSourceOutput(parameters, GenerateInterceptorSource);
    }

    private static void GenerateInterceptorSource(SourceProductionContext context, ILInterceptorParameters parameters)
    {
        var (methodBuilderInterceptionLocations, constructorBuilderInterceptorLocations) = parameters;
        if (methodBuilderInterceptionLocations.IsEmpty && constructorBuilderInterceptorLocations.IsEmpty)
            return;

        var source = new StringBuilder();
        ILGeneratorInterceptorsRenderer.Render(source, methodBuilderInterceptionLocations, constructorBuilderInterceptorLocations);

        context.AddSource("ILGeneratorInterceptors", source.ToString());
    }

    private static bool OfGetILGeneratorInvocation(SyntaxNode node, CancellationToken cancellationToken) => node is InvocationExpressionSyntax
    {
        Expression: MemberAccessExpressionSyntax
        {
            Name.Identifier.ValueText: "GetILGenerator"
        }
    };

    private static InterceptionLocationInfo ToMethodBuilderInterceptionLocationInfo(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        return ToInterceptionLocationInfo(context, "MethodBuilder", cancellationToken);
    }

    private static InterceptionLocationInfo ToConstructorBuilderInterceptionLocationInfo(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        return ToInterceptionLocationInfo(context, "ConstructorBuilder", cancellationToken);
    }

    private static InterceptionLocationInfo ToInterceptionLocationInfo(GeneratorSyntaxContext context, string receiverType, CancellationToken cancellationToken)
    {
        var node = (InvocationExpressionSyntax)context.Node;
        IOperation? operation = context.SemanticModel.GetOperation(node, cancellationToken);

        if (operation is not IInvocationOperation { Arguments.Length: 0 } invocation || invocation.Instance?.Type?.Name != receiverType)
            return default;

        LinePosition position = ((MemberAccessExpressionSyntax)node.Expression)
            .Name
            .GetLocation()
            .GetLineSpan()
            .StartLinePosition;

        return new InterceptionLocationInfo(node.SyntaxTree.FilePath, position.Line + 1, position.Character + 1);
    }
}