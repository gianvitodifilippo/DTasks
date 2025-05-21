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
public class AutoConfigurationGenerator : IIncrementalGenerator
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

    private static void EmitSource(SourceProductionContext context, ImmutableArray<InfrastructureMarshalingBuilderInvocation> invocations)
    {
        StringBuilder sb = new();
        MarshalingConfigurationSourceBuilder sourceBuilder = new(sb);
        
        sourceBuilder.Begin();

        foreach (InfrastructureMarshalingBuilderInvocation invocation in invocations.Distinct())
        {
            sourceBuilder.AddInvocation(invocation);
        }
        
        sourceBuilder.End();

        string source = sb.ToString();
        context.AddSource("DTasks.Analyzer.Marshaling.g.cs", source);
    }
    
    private static bool OfTriggeringSyntax(SyntaxNode node, CancellationToken cancellationToken)
    {
        return node is
            MethodDeclarationSyntax or
            VariableDeclaratorSyntax or
            InvocationExpressionSyntax or
            ParameterSyntax;
    }

    private static InfrastructureMarshalingBuilderInvocation ToBuilderInvocation(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        SemanticModel semanticModel = context.SemanticModel;
        
        switch (context.Node)
        {
            case MethodDeclarationSyntax methodDeclaration:
                return HandleMethodDeclaration(methodDeclaration, semanticModel, cancellationToken);
            
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

    private static InfrastructureMarshalingBuilderInvocation HandleMethodDeclaration(MethodDeclarationSyntax methodDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (!methodDeclaration.IsAsync() || methodDeclaration.IsStatic() || methodDeclaration.Arity != 0)
            return default;
        
        ClassDeclarationSyntax? containingClass = methodDeclaration.GetContainingClass();
        if (containingClass is null || containingClass.Arity != 0)
            return default;
        
        IMethodSymbol? method = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);
        if (method is null || !method.IsDAsync())
            return default;

        ISymbol? symbol = semanticModel.GetDeclaredSymbol(containingClass, cancellationToken);
        if (symbol is not INamedTypeSymbol type)
            return default;

        return SurrogateDTaskOf(type);
    }

    private static InfrastructureMarshalingBuilderInvocation HandleInvocationExpression(InvocationExpressionSyntax invocationExpression, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        IOperation? operation = semanticModel.GetOperation(invocationExpression, cancellationToken);
        if (operation is not IInvocationOperation invocationOperation)
            return default;
        
        if (!invocationOperation.TargetMethod.IsGenericWhenAll(out ITypeSymbol? typeArgument))
            return default;

        return AwaitWhenAllOf(typeArgument);
    }

    private static InfrastructureMarshalingBuilderInvocation HandleVariableDeclarator(VariableDeclaratorSyntax variableDeclarator, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        MethodDeclarationSyntax? containingMethodDeclaration = variableDeclarator.GetContainingMethod();
        if (containingMethodDeclaration is null || !containingMethodDeclaration.IsAsync())
            return default;
        
        IMethodSymbol? containingMethodSymbol = semanticModel.GetDeclaredSymbol(containingMethodDeclaration, cancellationToken);
        if (containingMethodSymbol is null || !containingMethodSymbol.IsDAsync())
            return default;
        
        IOperation? operation = semanticModel.GetOperation(variableDeclarator, cancellationToken);
        if (operation is not IVariableDeclaratorOperation { Symbol: { Type: INamedTypeSymbol type } })
            return default;
        
        if (!type.IsGenericDTask(out ITypeSymbol? typeArgument))
            return default;
        
        return SurrogateDTaskOf(typeArgument);
    }

    private static InfrastructureMarshalingBuilderInvocation HandleParameter(ParameterSyntax parameter, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        ISymbol? symbol = ModelExtensions.GetDeclaredSymbol(semanticModel, parameter, cancellationToken);
        if (symbol is not IParameterSymbol { Type: INamedTypeSymbol type })
            return default;
        
        if (!type.IsGenericDTask(out ITypeSymbol? typeArgument))
            return default;
        
        return SurrogateDTaskOf(typeArgument);
    }

    private static InfrastructureMarshalingBuilderInvocation SurrogateDTaskOf(ITypeSymbol type)
    {
        return new(InfrastructureMarshalingBuilderMethod.SurrogateDTaskOf, type.GetFullName());
    }

    private static InfrastructureMarshalingBuilderInvocation AwaitWhenAllOf(ITypeSymbol type)
    {
        return new(InfrastructureMarshalingBuilderMethod.AwaitWhenAllOf, type.GetFullName());
    }
}