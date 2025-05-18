using System.Diagnostics;

namespace DTasks.Infrastructure.Marshaling.TypeSyntax;

internal sealed record QualifiedNameSpec(NameSpec Left, string Right) : NameSpec
{
    [DebuggerHidden]
    public override void Accept(ITypeSyntaxSpecVisitor visitor)
    {
        visitor.VisitQualifiedName(this);
    }

    [DebuggerHidden]
    public override TReturn Accept<TReturn>(ITypeSyntaxSpecVisitor<TReturn> visitor)
    {
        return visitor.VisitQualifiedName(this);
    }
}