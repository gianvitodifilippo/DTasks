using Microsoft.CodeAnalysis;

namespace DTasks.AspNetCore.Analyzer.Utils;

public static class SymbolExtensions
{
    private static readonly SymbolDisplayFormat s_fullNameFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);
    
    private const string DTasksAspNetCoreEndpointRouteBuilderExtensionsQualifiedName = "Microsoft.AspNetCore.Routing.DTasksAspNetCoreEndpointRouteBuilderExtensions";
    private const string TaskQualifiedName = "System.Threading.Tasks.Task";
    private const string DTaskQualifiedName = "DTasks.DTask";
    
    public static bool IsEndpointRouteBuilderExtensions(this ITypeSymbol type)
    {
        return type.QualifiedNameIs(DTasksAspNetCoreEndpointRouteBuilderExtensionsQualifiedName.AsSpan());
    }

    public static bool IsTask(this ITypeSymbol type)
    {
        return type.QualifiedNameIs(TaskQualifiedName.AsSpan());
    }

    public static bool IsDTask(this ITypeSymbol type)
    {
        return type.QualifiedNameIs(DTaskQualifiedName.AsSpan());
    }
    
    public static string GetFullName(this ITypeSymbol type) => type.ToDisplayString(s_fullNameFormat);

    public static bool QualifiedNameIs(this ITypeSymbol type, ReadOnlySpan<char> fullName)
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

    public static bool QualifiedNameIs(this INamespaceSymbol @namespace, ReadOnlySpan<char> fullName)
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