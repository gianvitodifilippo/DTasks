using System.Diagnostics;

namespace DTasks.Infrastructure.Marshaling.TypeSyntax;

internal sealed record SimpleNameSpec(string Identifier) : NameSpec
{
    [DebuggerHidden]
    public override void Accept(ITypeSyntaxSpecVisitor visitor)
    {
        visitor.VisitSimpleName(this);
    }

    [DebuggerHidden]
    public override TReturn Accept<TReturn>(ITypeSyntaxSpecVisitor<TReturn> visitor)
    {
        return visitor.VisitSimpleName(this);
    }
}