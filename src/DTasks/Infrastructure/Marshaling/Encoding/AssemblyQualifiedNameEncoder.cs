using System.Diagnostics;
using DTasks.Infrastructure.Marshaling.TypeSyntax;
using DTasks.Utils;

namespace DTasks.Infrastructure.Marshaling.Encoding;

internal sealed class AssemblyQualifiedNameEncoder : ITypeSyntaxSpecVisitor
{
    private AssemblyQualifiedNameWriter _writer;

    private AssemblyQualifiedNameEncoder(IBitBufferWriter bufferWriter)
    {
        _writer = new AssemblyQualifiedNameWriter(bufferWriter);
    }

    private void VisitAssemblyQualifiedName(AssemblyQualifiedName name)
    {
        VisitTypeFullName(name.TypeFullName);
        _writer.WriteAssemblyQualifiedNameSeparator();
        VisitAssemblyFullName(name.AssemblyFullName);
    }

    private void VisitTypeFullName(TypeFullName fullName)
    {
        fullName.Namespace?.Accept(this);
        _writer.WriteQualifiedNameSeparator();
        VisitTypeName(fullName.Name);
    }

    private void VisitTypeName(TypeNameSpec name)
    {
        name.Accept(this);
    }

    private void VisitAssemblyFullName(AssemblyFullName fullName)
    {
        fullName.Name.Accept(this);
        _writer.WriteAssemblyPropertiesStart();
        _writer.WriteVersion(fullName.Version);
        _writer.WriteCulture(fullName.Culture);
        _writer.WritePublicKeyToken(fullName.PublicKeyToken);
    }
    
    void ITypeSyntaxSpecVisitor.VisitArrayTypeName(ArrayTypeNameSpec spec)
    {
        spec.ElementType.Accept(this);
        _writer.WriteArrayRank(spec.Rank);
    }

    void ITypeSyntaxSpecVisitor.VisitGenericTypeName(GenericTypeNameSpec spec)
    {
        spec.Definition.Accept(this);
        foreach (AssemblyQualifiedName genericArgument in spec.GenericArguments)
        {
            _writer.WriteGenericArgumentStart();
            VisitAssemblyQualifiedName(genericArgument);
        }
    }

    void ITypeSyntaxSpecVisitor.VisitNestedTypeIdentifier(NestedTypeIdentifierSpec spec)
    {
        spec.Left.Accept(this);
        _writer.WriteNestedTypeSeparator();
        ((ITypeSyntaxSpecVisitor)this).VisitSimpleTypeIdentifier(spec.Right);
    }

    void ITypeSyntaxSpecVisitor.VisitPointerTypeName(PointerTypeNameSpec spec)
    {
        throw new NotSupportedException("Pointer types cannot be marshalled.");
    }

    void ITypeSyntaxSpecVisitor.VisitQualifiedName(QualifiedNameSpec spec)
    {
        spec.Left.Accept(this);
        _writer.WriteQualifiedNameSeparator();
        _writer.WriteIdentifier(spec.Right);
    }

    void ITypeSyntaxSpecVisitor.VisitByRefTypeName(ByRefTypeNameSpec spec)
    {
        throw new NotSupportedException("By-ref types cannot be marshalled.");
    }

    void ITypeSyntaxSpecVisitor.VisitSimpleName(SimpleNameSpec spec)
    {
        _writer.WriteIdentifier(spec.Identifier);
    }

    void ITypeSyntaxSpecVisitor.VisitSimpleTypeIdentifier(SimpleTypeIdentifierSpec spec)
    {
        _writer.WriteIdentifier(spec.Identifier);
        if (spec.Arity != 0)
        {
            _writer.WriteGenericTypeArity(spec.Arity);
        }
    }

    public static void Encode(IBitBufferWriter bufferWriter, AssemblyQualifiedName name, TypeEncodingStrategy encodingStrategy)
    {
        var encoder = new AssemblyQualifiedNameEncoder(bufferWriter);
        switch (encodingStrategy)
        {
            case TypeEncodingStrategy.AssemblyQualifiedName:
                encoder.VisitAssemblyQualifiedName(name);
                break;
            
            case TypeEncodingStrategy.FullName:
                encoder.VisitTypeFullName(name.TypeFullName);
                break;
            
            case TypeEncodingStrategy.Name:
                encoder.VisitTypeName(name.TypeFullName.Name);
                break;
            
            default:
                throw new UnreachableException();
        }
    }
}