namespace DTasks.Infrastructure.Marshaling.TypeSyntax;

internal interface ITypeSyntaxSpecVisitor
{
    void VisitArrayTypeName(ArrayTypeNameSpec spec);
    void VisitGenericTypeName(GenericTypeNameSpec spec);
    void VisitNestedTypeIdentifier(NestedTypeIdentifierSpec spec);
    void VisitPointerTypeName(PointerTypeNameSpec spec);
    void VisitQualifiedName(QualifiedNameSpec spec);
    void VisitByRefTypeName(ByRefTypeNameSpec spec);
    void VisitSimpleName(SimpleNameSpec spec);
    void VisitSimpleTypeIdentifier(SimpleTypeIdentifierSpec spec);
}

internal interface ITypeSyntaxSpecVisitor<out TReturn>
{
    TReturn VisitArrayTypeName(ArrayTypeNameSpec spec);
    TReturn VisitGenericTypeName(GenericTypeNameSpec spec);
    TReturn VisitNestedTypeIdentifier(NestedTypeIdentifierSpec spec);
    TReturn VisitPointerTypeName(PointerTypeNameSpec spec);
    TReturn VisitQualifiedName(QualifiedNameSpec spec);
    TReturn VisitByRefTypeName(ByRefTypeNameSpec spec);
    TReturn VisitSimpleName(SimpleNameSpec spec);
    TReturn VisitSimpleTypeIdentifier(SimpleTypeIdentifierSpec spec);
}
