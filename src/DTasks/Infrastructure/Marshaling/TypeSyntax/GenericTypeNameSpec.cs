using System.Collections.Immutable;
using System.Diagnostics;

namespace DTasks.Infrastructure.Marshaling.TypeSyntax;

internal sealed record GenericTypeNameSpec(
    TypeIdentifierSpec Definition,
    ImmutableArray<AssemblyQualifiedName> GenericArguments) : NonByRefTypeNameSpec
{
    [DebuggerHidden]
    public override void Accept(ITypeSyntaxSpecVisitor visitor)
    {
        visitor.VisitGenericTypeName(this);
    }

    [DebuggerHidden]
    public override TReturn Accept<TReturn>(ITypeSyntaxSpecVisitor<TReturn> visitor)
    {
        return visitor.VisitGenericTypeName(this);
    }
}