using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace DTasks.Analyzer.Utils;

internal static class SymbolExtensions
{
    private static readonly SymbolDisplayFormat s_fullNameFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);
    
    private const string ActionQualifiedName = "System.Action";
    private const string DTaskQualifiedName = "DTasks.DTask";
    private const string ConfigurationBuilderQualifiedName = "DTasks.Configuration.IDTasksConfigurationBuilder";
    private const string ConfigurationBuilderAttributeQualifiedName = "DTasks.Metadata.ConfigurationBuilderAttribute";
    private const string WhenAllMethodName = "WhenAll";

    public static bool IsGenericWhenAll(this IMethodSymbol method, [NotNullWhen(true)] out ITypeSymbol? typeArgument)
    {
        if (method is not { Name: WhenAllMethodName, IsStatic: true, Arity: 1 })
        {
            typeArgument = null;
            return false;
        }

        typeArgument = method.TypeArguments[0];

        INamedTypeSymbol containingType = method.ContainingType;
        return containingType.IsDTask();
    }

    public static string GetFullName(this ITypeSymbol type) => type.ToDisplayString(s_fullNameFormat);

    public static bool IsAction(this ITypeSymbol type)
    {
        return type.QualifiedNameIs(ActionQualifiedName.AsSpan());
    }

    public static bool IsDTask(this INamedTypeSymbol type)
    {
        return type.Arity == 0 && type.QualifiedNameIs(DTaskQualifiedName.AsSpan());
    }
    
    public static bool IsGenericDTask(this INamedTypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? typeArgument)
    {
        if (type.Arity != 1 || !type.QualifiedNameIs(DTaskQualifiedName.AsSpan()))
        {
            typeArgument = null;
            return false;
        }

        typeArgument = type.TypeArguments[0];
        return true;
    }

    public static bool IsConfigurationBuilderAttribute(this ITypeSymbol type)
    {
        return type.QualifiedNameIs(ConfigurationBuilderAttributeQualifiedName.AsSpan());
    }

    public static bool IsAssignableToConfigurationBuilder(this ITypeSymbol type)
    {
        if (type.QualifiedNameIs(ConfigurationBuilderQualifiedName.AsSpan()))
            return true;

        foreach (INamedTypeSymbol interfaceType in type.Interfaces)
        {
            if (interfaceType.IsAssignableToConfigurationBuilder())
                return true;
        }

        return false;
    }
    
    private static bool QualifiedNameIs(this ITypeSymbol type, ReadOnlySpan<char> fullName)
    {
        string typeName = type.Name;
        
        if (type.ContainingNamespace is not { } @namespace)
            return fullName.SequenceEqual(type.Name.AsSpan());

        if (!fullName.EndsWith(typeName.AsSpan()))
            return false;

        int dotIndex = fullName.Length - typeName.Length - 1;
        if (dotIndex <= 0 || fullName[dotIndex] != '.')
            return false;

        return @namespace.QualifiedNameIs(fullName.Slice(0, dotIndex));
    }

    private static bool QualifiedNameIs(this INamespaceSymbol @namespace, ReadOnlySpan<char> fullName)
    {
        while (@namespace.ContainingNamespace is { } parentNamespace)
        {
            string namespaceName = @namespace.Name;

            if (parentNamespace.IsGlobalNamespace)
                return fullName.SequenceEqual(namespaceName.AsSpan());

            if (!fullName.EndsWith(namespaceName.AsSpan()))
                return false;

            int dotIndex = fullName.Length - namespaceName.Length - 1;
            if (dotIndex <= 0 || fullName[dotIndex] != '.')
                return false;

            @namespace = parentNamespace;
            fullName = fullName.Slice(0, dotIndex);
        }

        return false;
    }
}