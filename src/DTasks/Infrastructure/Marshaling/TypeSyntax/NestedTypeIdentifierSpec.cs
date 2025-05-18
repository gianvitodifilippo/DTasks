namespace DTasks.Infrastructure.Marshaling.TypeSyntax;

internal sealed record NestedTypeIdentifierSpec(TypeIdentifierSpec Left, SimpleTypeIdentifierSpec Right) : TypeIdentifierSpec
{
    public override void Accept(ITypeSyntaxSpecVisitor visitor)
    {
        visitor.VisitNestedTypeIdentifier(this);
    }

    public override TReturn Accept<TReturn>(ITypeSyntaxSpecVisitor<TReturn> visitor)
    {
        return visitor.VisitNestedTypeIdentifier(this);
    }
}