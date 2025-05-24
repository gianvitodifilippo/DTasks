using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DTasks.Analyzer.Configuration;
using DTasks.Analyzer.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DTasks.Analyzer;

[Generator]
public sealed class AutoConfigureInterceptorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var locations = context.SyntaxProvider
            .CreateSyntaxProvider(OfMethodCalls, ToMethodInfoAndLocation)
            .Where(static location => location != default)
            .Collect();
        
        context.RegisterImplementationSourceOutput(locations, EmitSource!);
    }

    private static void EmitSource(SourceProductionContext context, ImmutableArray<(InterceptableConfigurationBuilderMethodInfo MethodInfo, InterceptableLocation Location)> infoAndLocations)
    {
        var infos = infoAndLocations
            .GroupBy(infoAndLocation => infoAndLocation.MethodInfo)
            .Select(infoAndLocationGroup => (infoAndLocationGroup.Key, infoAndLocationGroup.Select(grouping => grouping.Location)));
        
        string source = AutoConfigureInterceptorSourceBuilder.GetSource(infos);
        context.AddSource("DTasks.Analyzer.Interceptors.g.cs", source);
    }

    private static bool OfMethodCalls(SyntaxNode node, CancellationToken cancellationToken)
    {
        return node.IsKind(SyntaxKind.InvocationExpression);
    }

    private static (InterceptableConfigurationBuilderMethodInfo, InterceptableLocation) ToMethodInfoAndLocation(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.Node is not InvocationExpressionSyntax invocationExpression)
        {
            Debug.Fail("Expected invocation expression.");
            return default;
        }
        
        SemanticModel semanticModel = context.SemanticModel;
        SymbolInfo methodSymbolInfo = semanticModel.GetSymbolInfo(invocationExpression.Expression, cancellationToken);
        
        ISymbol? methodOrCandidateSymbol = methodSymbolInfo.Symbol;
        if (methodOrCandidateSymbol is null && methodSymbolInfo is {CandidateSymbols.Length: 1, CandidateReason: CandidateReason.OverloadResolutionFailure})
        {
            // This happens when calling methods that are source-generated like AutoConfigure()
            methodOrCandidateSymbol = methodSymbolInfo.CandidateSymbols[0];
        }
        if (methodOrCandidateSymbol is not IMethodSymbol method)
            return default;

        if (!TryGetConfigurationBuilderParameter(method, out IParameterSymbol? configurationBuilderParameter))
            return default;

        InterceptableLocation? location = semanticModel.GetInterceptableLocation(invocationExpression);
        if (location is null)
            return default;

        var parameterArrayBuilder = ImmutableArray.CreateBuilder<string>(method.IsExtensionMethod
            ? method.Parameters.Length + 1
            : method.Parameters.Length);

        if (method is { IsExtensionMethod: true, ReceiverType: not null })
        {
            parameterArrayBuilder.Add(method.ReceiverType.GetFullName());
        }
        
        foreach (IParameterSymbol parameter in method.Parameters)
        {
            parameterArrayBuilder.Add(parameter.Type.GetFullName());
        }

        InterceptableConfigurationBuilderMethodInfo methodInfo = new(
            method.IsStatic || method.IsExtensionMethod,
            method.IsExtensionMethod,
            method.Name,
            method.ContainingType.GetFullName(),
            parameterArrayBuilder.ToImmutable().ToEquatable(),
            method.ReturnType.GetFullName(),
            method.IsExtensionMethod
                ? configurationBuilderParameter.Ordinal + 1
                : configurationBuilderParameter.Ordinal);
        
        return (methodInfo, location);
    }

    private static bool TryGetConfigurationBuilderParameter(IMethodSymbol method, [NotNullWhen(true)] out IParameterSymbol? configurationBuilderParameter)
    {
        configurationBuilderParameter = null;
        
        foreach (IParameterSymbol parameter in method.Parameters)
        {
            if (!parameter.Type.IsAction())
                continue;
            
            var parameterType = (INamedTypeSymbol)parameter.Type;
            if (parameterType.Arity != 1)
                continue;
            
            if (!parameterType.TypeArguments[0].IsAssignableToConfigurationBuilder())
                continue;
            
            ImmutableArray<AttributeData> attributes = parameter.GetAttributes();
            foreach (AttributeData attribute in attributes)
            {
                if (attribute.AttributeClass is null || !attribute.AttributeClass.IsConfigurationBuilderAttribute())
                    continue;

                if (configurationBuilderParameter is not null)
                {
                    configurationBuilderParameter = null;
                    return false;
                }
                
                configurationBuilderParameter = parameter;
                break;
            }
        }
        
        return configurationBuilderParameter is not null;
    }
}