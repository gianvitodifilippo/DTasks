namespace DTasks.Infrastructure.Marshaling.TypeSyntax;

internal sealed record AssemblyFullName(
    NameSpec Name,
    Version Version,
    string Culture,
    ulong? PublicKeyToken);
