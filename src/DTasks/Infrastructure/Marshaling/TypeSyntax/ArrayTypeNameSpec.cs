using System.Diagnostics;

namespace DTasks.Infrastructure.Marshaling.TypeSyntax;

internal sealed record ArrayTypeNameSpec(NonByRefTypeNameSpec ElementType, int Rank) : NonByRefTypeNameSpec
{
    [DebuggerHidden]
    public override void Accept(ITypeSyntaxSpecVisitor visitor)
    {
        visitor.VisitArrayTypeName(this);
    }

    [DebuggerHidden]
    public override TReturn Accept<TReturn>(ITypeSyntaxSpecVisitor<TReturn> visitor)
    {
        return visitor.VisitArrayTypeName(this);
    }
}