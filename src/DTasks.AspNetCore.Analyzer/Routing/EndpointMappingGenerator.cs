using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using DTasks.AspNetCore.Analyzer.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace DTasks.AspNetCore.Analyzer.Routing;

[Generator]
public sealed class EndpointMappingGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<ImmutableArray<MapMethodInvocation>> invocationProviders = context.SyntaxProvider
            .CreateSyntaxProvider(OfCallsToMapAsyncMethods, ToMethodInvocation)
            .Where(parameters => parameters is not null)
            .Collect()!;
        
        context.RegisterImplementationSourceOutput(invocationProviders, EmitSource);
    }

    private static void EmitSource(SourceProductionContext context, ImmutableArray<MapMethodInvocation> invocations)
    {
        StringBuilder sb = new();
        HttpMappingSourceBuilder sourceBuilder = new(sb);
        
        sourceBuilder.Begin();
        foreach (MapMethodInvocation invocation in invocations)
        {
            sourceBuilder.EmitMapMethod(invocation);
        }
        sourceBuilder.End();

        string source = sb.ToString();
        context.AddSource("DTasks.AspNetCore.Analyzer.Routing.g.cs", source);
    }

    private static bool OfCallsToMapAsyncMethods(SyntaxNode node, CancellationToken cancellationToken)
    {
        return node is InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax
            {
                Name.Identifier.Text:
                    "MapAsyncGet" or 
                    "MapAsyncPost" or 
                    "MapAsyncPut" or
                    "MapAsyncDelete" or
                    "MapAsyncPatch" or
                    "MapAsync"
            }
        };
    }

    private static MapMethodInvocation? ToMethodInvocation(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.Node is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccessExpression } invocationExpression)
        {
            Debug.Fail($"Expected node filter by {nameof(OfCallsToMapAsyncMethods)}.");
            return null;
        }

        MapMethod method;
        switch (memberAccessExpression.Name.Identifier.Text)
        {
            case "MapAsyncGet":
                method = MapMethod.Get;
                break;
            
            case "MapAsyncPost":
                method = MapMethod.Post;
                break;
            
            case "MapAsyncPut":
                method = MapMethod.Put;
                break;
            
            case "MapAsyncDelete":
                method = MapMethod.Delete;
                break;
            
            case "MapAsyncPatch":
                method = MapMethod.Patch;
                break;
            
            case "MapAsync":
                method = MapMethod.All;
                break;
            
            default:
                return null;
        }

        SymbolInfo methodSymbolInfo = context.SemanticModel.GetSymbolInfo(memberAccessExpression, cancellationToken);
        if (methodSymbolInfo.Symbol is not IMethodSymbol mapMethod)
            return null;
        
        if (!mapMethod.ContainingType.IsEndpointRouteBuilderExtensions())
            return null;

        string? patternTypeFullName = "string";
        IOperation? patternOperation = context.SemanticModel.GetOperation(invocationExpression.ArgumentList.Arguments[0], cancellationToken);
        if (patternOperation is IArgumentOperation { Parameter.Type: { } patternType })
        {
            patternTypeFullName = patternType.GetFullName();
        }
        
        SymbolInfo lambdaSymbolInfo = context.SemanticModel.GetSymbolInfo(invocationExpression.ArgumentList.Arguments[1].Expression);
        if (lambdaSymbolInfo.Symbol is not IMethodSymbol lambdaMethod)
            return null;

        if (!lambdaMethod.ReturnType.IsTask() && !lambdaMethod.ReturnType.IsDTask())
            return null;

        int parameterCount = lambdaMethod.Parameters.Length;
        ParameterInfo[] parameterInfos = new ParameterInfo[parameterCount];

        for (int i = 0; i < parameterCount; i++)
        {
            IParameterSymbol parameter = lambdaMethod.Parameters[i];
            ImmutableArray<AttributeData> parameterAttributes = parameter.GetAttributes();
            
            string parameterName = parameter.Name;
            string parameterTypeFullName = parameter.Type.GetFullName();
            if (parameterAttributes.Length == 0)
            {
                parameterInfos[i] = new(parameterName, parameterTypeFullName, null);
                continue;
            }
            
            // TODO: Support multiple attributes and their arguments
            AttributeData attribute = parameterAttributes[0];
            if (attribute.AttributeClass is not { } attributeType)
            {
                parameterInfos[i] = new(parameterName, parameterTypeFullName, null);
                continue;
            }

            parameterInfos[i] = new(parameterName, parameterTypeFullName, attributeType.GetFullName());
        }

        var taskType = (INamedTypeSymbol)lambdaMethod.ReturnType;
        string? resultTypeFullName = taskType.Arity == 0
            ? null
            : taskType.TypeArguments[0].GetFullName();

        return new(method, patternTypeFullName, resultTypeFullName, parameterInfos.ToEquatable());
    }
}