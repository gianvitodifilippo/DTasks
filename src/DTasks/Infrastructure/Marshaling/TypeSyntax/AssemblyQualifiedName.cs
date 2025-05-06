namespace DTasks.Infrastructure.Marshaling.TypeSyntax;

internal sealed record AssemblyQualifiedName(
    TypeFullName TypeFullName,
    AssemblyFullName AssemblyFullName)
{
    public static AssemblyQualifiedName Parse(string text)
    {
        return new TypeSyntaxParser(text).Parse();
    }
}
