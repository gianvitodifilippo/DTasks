using System.Diagnostics;

namespace DTasks.Infrastructure.Marshaling.TypeSyntax;

internal sealed record ByRefTypeNameSpec(NonByRefTypeNameSpec ElementType) : TypeNameSpec
{
    [DebuggerHidden]
    public override void Accept(ITypeSyntaxSpecVisitor visitor)
    {
        visitor.VisitByRefTypeName(this);
    }

    [DebuggerHidden]
    public override TReturn Accept<TReturn>(ITypeSyntaxSpecVisitor<TReturn> visitor)
    {
        return visitor.VisitByRefTypeName(this);
    }
}
