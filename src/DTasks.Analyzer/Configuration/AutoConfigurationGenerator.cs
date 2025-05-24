using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using DTasks.Analyzer.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace DTasks.Analyzer.Configuration;

[Generator]
public sealed class AutoConfigurationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // TODO: Display warnings for generic methods/types
        
        var invocations = context.SyntaxProvider
            .CreateSyntaxProvider(OfTriggeringSyntax, ToBuilderInvocation)
            .Where(invocation => invocation != default)
            .Collect();
        
        context.RegisterImplementationSourceOutput(invocations, EmitSource);
    }

    private static void EmitSource(SourceProductionContext context, ImmutableArray<ConfigurationBuilderInvocation> invocations)
    {
        StringBuilder sb = new();
        ConfigurationSourceBuilder sourceBuilder = new(sb);
        
        sourceBuilder.Begin();
        sourceBuilder.AddInfrastructureBuilderInvocations(invocations.Distinct()); // TODO: Move in pipeline
        sourceBuilder.End();

        string source = sb.ToString();
        context.AddSource("DTasks.Analyzer.Configuration.g.cs", source);
    }
    
    private static bool OfTriggeringSyntax(SyntaxNode node, CancellationToken cancellationToken)
    {
        return node is
            VariableDeclaratorSyntax or
            InvocationExpressionSyntax or
            ParameterSyntax;
    }

    private static ConfigurationBuilderInvocation ToBuilderInvocation(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        SemanticModel semanticModel = context.SemanticModel;
        
        switch (context.Node)
        {
            case VariableDeclaratorSyntax variableDeclarator:
                return HandleVariableDeclarator(variableDeclarator, semanticModel, cancellationToken);

            case InvocationExpressionSyntax invocationExpression:
                return HandleInvocationExpression(invocationExpression, semanticModel, cancellationToken);
            
            case ParameterSyntax parameter:
                return HandleParameter(parameter, semanticModel, cancellationToken);
            
            default:
                Debug.Fail("Unhandled syntax type.");
                break;
        }

        return default;
    }

    private static ConfigurationBuilderInvocation HandleInvocationExpression(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        IOperation? operation = semanticModel.GetOperation(invocationExpression, cancellationToken);
        if (operation is not IInvocationOperation invocationOperation)
            return default;
        
        if (!invocationOperation.TargetMethod.IsGenericWhenAll(out ITypeSymbol? typeArgument))
            return default;

        return AwaitWhenAllOf(typeArgument);
    }

    private static ConfigurationBuilderInvocation HandleVariableDeclarator(VariableDeclaratorSyntax variableDeclarator, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        IMethodSymbol? containerSymbol;
        
        MethodDeclarationSyntax? containingMethod = variableDeclarator.GetContainingMethod();
        if (containingMethod is not null)
        {
            if (!containingMethod.IsAsync())
                return default;
            
            containerSymbol = semanticModel.GetDeclaredSymbol(containingMethod, cancellationToken);
        }
        else
        {
            LambdaExpressionSyntax? containingLambda = variableDeclarator.GetContainingLambda();
            if (containingLambda is null || !containingLambda.IsAsync())
                return default;
            
            SymbolInfo lambdaSymbolInfo = semanticModel.GetSymbolInfo(containingLambda, cancellationToken);
            containerSymbol = lambdaSymbolInfo.Symbol as IMethodSymbol;
        }
        
        if (containerSymbol is null)
            return default;
        
        IOperation? operation = semanticModel.GetOperation(variableDeclarator, cancellationToken);
        if (operation is not IVariableDeclaratorOperation { Symbol.Type: INamedTypeSymbol type })
            return default;
        
        if (!type.IsGenericDTask(out ITypeSymbol? typeArgument))
            return default;
        
        return SurrogateDTaskOf(typeArgument);
    }

    private static ConfigurationBuilderInvocation HandleParameter(ParameterSyntax parameter, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        ISymbol? symbol = ModelExtensions.GetDeclaredSymbol(semanticModel, parameter, cancellationToken);
        if (symbol is not IParameterSymbol { Type: INamedTypeSymbol type })
            return default;
        
        if (!type.IsGenericDTask(out ITypeSymbol? typeArgument))
            return default;
        
        return SurrogateDTaskOf(typeArgument);
    }

    private static ConfigurationBuilderInvocation SurrogateDTaskOf(ITypeSymbol type)
    {
        return new(ConfigurationBuilderMethod.SurrogateDTaskOf, type.GetFullName());
    }

    private static ConfigurationBuilderInvocation AwaitWhenAllOf(ITypeSymbol type)
    {
        return new(ConfigurationBuilderMethod.AwaitWhenAllOf, type.GetFullName());
    }
}