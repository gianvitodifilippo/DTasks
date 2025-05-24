using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace DTasks.Tests.Interceptors.Emit;

[Generator]
public class EmitInterceptorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var methodBuilderInterceptableLocations = context.SyntaxProvider
            .CreateSyntaxProvider(OfGetILGeneratorInvocation, ToMethodBuilderInterceptableLocation)
            .Where(static location => location is not null)
            .Collect();

        var constructorBuilderInterceptableLocations = context.SyntaxProvider
            .CreateSyntaxProvider(OfGetILGeneratorInvocation, ToConstructorBuilderInterceptableLocation)
            .Where(static location => location is not null)
            .Collect();

        var interceptableLocations = methodBuilderInterceptableLocations
            .Combine(constructorBuilderInterceptableLocations);

        context.RegisterImplementationSourceOutput(interceptableLocations, GenerateInterceptorSource!);
    }

    private static void GenerateInterceptorSource(SourceProductionContext context, (ImmutableArray<InterceptableLocation>, ImmutableArray<InterceptableLocation>) parameters)
    {
        var (methodBuilderInterceptionLocations, constructorBuilderInterceptorLocations) = parameters;
        if (methodBuilderInterceptionLocations.IsEmpty && constructorBuilderInterceptorLocations.IsEmpty)
            return;

        StringBuilder sb = new();
        ILGeneratorInterceptorsRenderer.Render(sb, methodBuilderInterceptionLocations, constructorBuilderInterceptorLocations);

        string source = sb.ToString();
        context.AddSource("ILGeneratorInterceptors.g.cs", source);
    }

    private static bool OfGetILGeneratorInvocation(SyntaxNode node, CancellationToken cancellationToken) => node is InvocationExpressionSyntax
    {
        Expression: MemberAccessExpressionSyntax
        {
            Name.Identifier.ValueText: "GetILGenerator"
        }
    };

    private static InterceptableLocation? ToMethodBuilderInterceptableLocation(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        return ToInterceptableLocation(context, "MethodBuilder", cancellationToken);
    }

    private static InterceptableLocation? ToConstructorBuilderInterceptableLocation(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        return ToInterceptableLocation(context, "ConstructorBuilder", cancellationToken);
    }

    private static InterceptableLocation? ToInterceptableLocation(GeneratorSyntaxContext context, string receiverType, CancellationToken cancellationToken)
    {
        var node = (InvocationExpressionSyntax)context.Node;
        IOperation? operation = context.SemanticModel.GetOperation(node, cancellationToken);

        if (operation is not IInvocationOperation { Arguments.Length: 0 } invocation || invocation.Instance?.Type?.Name != receiverType)
            return null;

        return context.SemanticModel.GetInterceptableLocation(node, cancellationToken);
    }
}