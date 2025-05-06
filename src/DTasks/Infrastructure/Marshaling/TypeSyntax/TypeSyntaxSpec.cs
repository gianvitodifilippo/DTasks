namespace DTasks.Infrastructure.Marshaling.TypeSyntax;

internal abstract record TypeSyntaxSpec
{
    public abstract void Accept(ITypeSyntaxSpecVisitor visitor);
    
    public abstract TReturn Accept<TReturn>(ITypeSyntaxSpecVisitor<TReturn> visitor);
}