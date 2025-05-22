using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DTasks.Analyzer.Utils;

public static class SyntaxExtensions
{
    public static bool IsAsync(this MethodDeclarationSyntax methodDeclaration)
    {
        return methodDeclaration.Modifiers.Any(SyntaxKind.AsyncKeyword);
    }
    
    public static bool IsStatic(this MethodDeclarationSyntax methodDeclaration)
    {
        return methodDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword);
    }
    
    public static MethodDeclarationSyntax? GetContainingMethod(this SyntaxNode? node)
    {
        while (node is not CompilationUnitSyntax and not null)
        {
            if (node is MethodDeclarationSyntax methodDeclaration)
                return methodDeclaration;

            node = node.Parent;
        }

        return null;
    }

    public static ClassDeclarationSyntax? GetContainingClass(this SyntaxNode? node)
    {
        while (node is not CompilationUnitSyntax and not null)
        {
            if (node is ClassDeclarationSyntax classDeclaration)
                return classDeclaration;
            
            node = node.Parent;
        }

        return null;
    }
}