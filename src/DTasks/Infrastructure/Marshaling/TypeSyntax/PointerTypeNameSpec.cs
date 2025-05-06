using System.Diagnostics;

namespace DTasks.Infrastructure.Marshaling.TypeSyntax;

internal sealed record PointerTypeNameSpec(NonByRefTypeNameSpec ElementType) : NonByRefTypeNameSpec
{
    [DebuggerHidden]
    public override void Accept(ITypeSyntaxSpecVisitor visitor)
    {
        visitor.VisitPointerTypeName(this);
    }

    [DebuggerHidden]
    public override TReturn Accept<TReturn>(ITypeSyntaxSpecVisitor<TReturn> visitor)
    {
        return visitor.VisitPointerTypeName(this);
    }
}
