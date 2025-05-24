using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using DTasks.AspNetCore.Analyzer.Configuration;
using DTasks.AspNetCore.Analyzer.Routing;
using DTasks.AspNetCore.Analyzer.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace DTasks.AspNetCore.Analyzer;

[Generator]
public sealed class DTasksAspNetCoreGenerator : IIncrementalGenerator
{
    private const string ResumptionEndpointAttributeQualifiedName = "DTasks.AspNetCore.Metadata.ResumptionEndpointsAttribute";
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<MapMethodInvocation> invocations = context.SyntaxProvider
            .CreateSyntaxProvider(OfCallsToMapAsyncMethods, ToMethodInvocation)
            .Where(static parameters => parameters is not null)!;
        
        IncrementalValueProvider<ImmutableArray<string>> serviceTypeLists = invocations
            .SelectMany(static (invocation, _) => invocation.Parameters)
            .Where(static parameter => parameter.Binding == "global::Microsoft.AspNetCore.Mvc.FromServicesAttribute")
            .Select(static (parameter, _) => parameter.Type)
            .Collect();

        IncrementalValueProvider<ImmutableArray<string>> invocationResultTypeLists = invocations
            .Select(static (invocation, _) => invocation.ResultType)
            .Where(static resultType => resultType is not null and not "global::Microsoft.AspNetCore.Http.IResult")
            .Collect()!;
        
        IncrementalValueProvider<ImmutableArray<string>> resultTypeLists = context.SyntaxProvider
            .CreateSyntaxProvider(OfMethodCallsOrPropertyAccesses, ToAsyncResultTypes)
            .SelectMany(static (resultTypes, _) => resultTypes)
            .Collect();

        IncrementalValueProvider<ImmutableArray<ResumptionEndpointContainer>> containerLists = context.SyntaxProvider
            .ForAttributeWithMetadataName(ResumptionEndpointAttributeQualifiedName, OfStaticClasses, ToResumptionEndpointsContainer)
            .Collect();

        var autoConfigurationData = serviceTypeLists
            .Combine(invocationResultTypeLists)
            .Combine(resultTypeLists)
            .Combine(containerLists);

        context.RegisterImplementationSourceOutput(invocations.Collect(), EmitMappingMethodsSource);
        context.RegisterImplementationSourceOutput(autoConfigurationData, EmitAutoConfigurationSource);
    }

    private static void EmitMappingMethodsSource(SourceProductionContext context, ImmutableArray<MapMethodInvocation> invocations)
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

    private static void EmitAutoConfigurationSource(SourceProductionContext context, (((ImmutableArray<string>, ImmutableArray<string>), ImmutableArray<string>), ImmutableArray<ResumptionEndpointContainer>) data)
    {
        (((ImmutableArray<string> serviceTypes, ImmutableArray<string> invocationResultTypes), ImmutableArray<string> resultTypes), ImmutableArray<ResumptionEndpointContainer> resumptionEndpoints) = data;
        
        StringBuilder sb = new();
        ConfigurationSourceBuilder sourceBuilder = new(sb);

        sourceBuilder.Begin();
        if (!serviceTypes.IsEmpty)
        {
            sourceBuilder.AddServiceTypes(serviceTypes.Distinct());
        }

        if (!invocationResultTypes.IsEmpty || !resultTypes.IsEmpty)
        {
            sourceBuilder.AddResultTypes(invocationResultTypes.Concat(resultTypes).Distinct());
        }
        
        sourceBuilder.AddResumptionEndpoints(resumptionEndpoints);
        sourceBuilder.End();
        
        string source = sb.ToString();
        context.AddSource("DTasks.AspNetCore.Analyzer.Configuration.g.cs", source);
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
        
        SemanticModel semanticModel = context.SemanticModel;

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

        SymbolInfo methodSymbolInfo = ModelExtensions.GetSymbolInfo(semanticModel, memberAccessExpression, cancellationToken);
        if (methodSymbolInfo.Symbol is not IMethodSymbol mapMethod)
            return null;
        
        if (!mapMethod.ContainingType.IsEndpointRouteBuilderExtensions())
            return null;

        string? patternTypeFullName = "string";
        IOperation? patternOperation = semanticModel.GetOperation(invocationExpression.ArgumentList.Arguments[0], cancellationToken);
        if (patternOperation is IArgumentOperation { Parameter.Type: { } patternType })
        {
            patternTypeFullName = patternType.GetFullName();
        }
        
        SymbolInfo lambdaSymbolInfo = ModelExtensions.GetSymbolInfo(semanticModel, invocationExpression.ArgumentList.Arguments[1].Expression);
        if (lambdaSymbolInfo.Symbol is not IMethodSymbol lambdaMethod)
            return null; // TODO: What if it's not a lambda?

        if (!lambdaMethod.ReturnType.IsTask() && !lambdaMethod.ReturnType.IsDTask())
            return null;

        int parameterCount = lambdaMethod.Parameters.Length;
        var parameterInfoArrayBuilder = ImmutableArray.CreateBuilder<ParameterInfo>(parameterCount);

        for (int i = 0; i < parameterCount; i++)
        {
            IParameterSymbol parameter = lambdaMethod.Parameters[i];
            ImmutableArray<AttributeData> parameterAttributes = parameter.GetAttributes();
            
            string parameterName = parameter.Name;
            string parameterTypeFullName = parameter.Type.GetFullName();
            if (parameterAttributes.Length == 0)
            {
                parameterInfoArrayBuilder.Add(new(parameterName, parameterTypeFullName, null));
                continue;
            }
            
            // TODO: Support multiple attributes and their arguments
            AttributeData attribute = parameterAttributes[0];
            if (attribute.AttributeClass is not { } attributeType)
            {
                parameterInfoArrayBuilder.Add(new(parameterName, parameterTypeFullName, null));
                continue;
            }

            parameterInfoArrayBuilder.Add(new(parameterName, parameterTypeFullName, attributeType.GetFullName()));
        }

        var taskType = (INamedTypeSymbol)lambdaMethod.ReturnType;
        string? resultTypeFullName = taskType.Arity == 0
            ? null
            : taskType.TypeArguments[0].GetFullName();

        return new(method, patternTypeFullName, resultTypeFullName, parameterInfoArrayBuilder.ToImmutable().ToEquatable());
    }

    private static bool OfMethodCallsOrPropertyAccesses(SyntaxNode node, CancellationToken cancellationToken)
    {
        return node is InvocationExpressionSyntax or MemberAccessExpressionSyntax;
    }

    private static EquatableArray<string> ToAsyncResultTypes(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        IOperation? operation = context.SemanticModel.GetOperation(context.Node, cancellationToken);

        IMethodSymbol? method = operation switch
        {
            IInvocationOperation invocation => invocation.TargetMethod,
            IPropertyReferenceOperation propertyReference => propertyReference.Property.GetMethod,
            _ => null
        };
        
        if (method is null)
            return default;

        ImmutableArray<string>.Builder? resultTypeArrayBuilder = null;
        ImmutableArray<AttributeData> attributes = method.GetReturnTypeAttributes();
        foreach (AttributeData attribute in attributes)
        {
            if (attribute.AttributeClass is null || !attribute.AttributeClass.IsAsyncResultAttribute())
                continue;
            
            ImmutableArray<TypedConstant> constructorArguments = attribute.ConstructorArguments;
            if (constructorArguments.Length < 1)
                continue;
            
            object? constructorArgumentValue = constructorArguments[0].Value;
            switch (constructorArgumentValue)
            {
                case int genericParameterPosition:
                    ImmutableArray<ITypeSymbol> typeArguments = method.TypeArguments;
                    if (typeArguments.Length <= genericParameterPosition)
                        continue;

                    resultTypeArrayBuilder ??= ImmutableArray.CreateBuilder<string>();
                    resultTypeArrayBuilder.Add(typeArguments[genericParameterPosition].GetFullName());
                    break;
                
                case ITypeSymbol resultType:
                    resultTypeArrayBuilder ??= ImmutableArray.CreateBuilder<string>();
                    resultTypeArrayBuilder.Add(resultType.GetFullName());
                    break;
            }
        }
        
        return resultTypeArrayBuilder is null
            ? default
            : resultTypeArrayBuilder.ToImmutable().ToEquatable();
    }

    private static bool OfStaticClasses(SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node is not ClassDeclarationSyntax classDeclaration)
            return false;

        foreach (SyntaxToken modifier in classDeclaration.Modifiers)
        {
            if (modifier.IsKind(SyntaxKind.StaticKeyword))
                return true;
        }
        
        return false;
    }

    private static ResumptionEndpointContainer ToResumptionEndpointsContainer(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not INamedTypeSymbol endpointsClass)
            return default;

        var memberArrayBuilder = ImmutableArray.CreateBuilder<string>();
        foreach (ISymbol member in endpointsClass.GetMembers())
        {
            bool isResumptionEndpoint = member is
                IFieldSymbol { IsReadOnly: true, DeclaredAccessibility: Accessibility.Public or Accessibility.Internal } or
                IPropertySymbol { DeclaredAccessibility: Accessibility.Public or Accessibility.Internal };

            if (!isResumptionEndpoint)
                continue;
            
            memberArrayBuilder.Add(member.Name);
        }
        
        return new(endpointsClass.GetFullName(), memberArrayBuilder.ToImmutable().ToEquatable());
    }
}