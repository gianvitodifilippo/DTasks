namespace DTasks.Infrastructure.Marshaling.TypeSyntax;

internal sealed record TypeFullName(
    NameSpec? Namespace,
    TypeNameSpec Name);
