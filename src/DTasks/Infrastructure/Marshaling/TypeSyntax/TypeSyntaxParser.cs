using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace DTasks.Infrastructure.Marshaling.TypeSyntax;

internal ref struct TypeSyntaxParser(string text)
{
    private TypeSyntaxLexer _lexer = new(text);

    public AssemblyQualifiedName Parse()
    {
        try
        {
            return ParseCore();
        }
        catch (TypeSyntaxParseException ex)
        {
            throw new FormatException($"'{text}' is an invalid assembly qualified name.", ex);
        }
    }

    private AssemblyQualifiedName ParseCore()
    {
        if (_lexer.Kind is not TypeSyntaxTokenKind.EndOfText)
            throw new TypeSyntaxParseException();

        var assemblyQualifiedName = AssemblyQualifiedName();
        Consume(TypeSyntaxTokenKind.StartOfText);
        
        return assemblyQualifiedName;
    }

    private AssemblyQualifiedName AssemblyQualifiedName()
    {
        var assemblyFullName = AssemblyFullName();

        Consume(TypeSyntaxTokenKind.Space);
        Consume(TypeSyntaxTokenKind.Comma);
        
        var typeFullName = TypeFullName();
        
        return new AssemblyQualifiedName(typeFullName, assemblyFullName);
    }

    private AssemblyFullName AssemblyFullName()
    {
        ulong? publicKeyToken = AssemblyPublicKeyToken();
        
        Consume(TypeSyntaxTokenKind.Space);
        Consume(TypeSyntaxTokenKind.Comma);

        string culture = AssemblyCulture();
        
        Consume(TypeSyntaxTokenKind.Space);
        Consume(TypeSyntaxTokenKind.Comma);
        
        Version version = AssemblyVersion();
        
        Consume(TypeSyntaxTokenKind.Space);
        Consume(TypeSyntaxTokenKind.Comma);
        
        NameSpec name = Name();
        
        return new AssemblyFullName(name, version, culture, publicKeyToken);
    }

    private ulong? AssemblyPublicKeyToken()
    {
        const string nullPublicKeyToken = "null";
        ulong? publicKeyToken;
        
        Consume(TypeSyntaxTokenKind.Word);
        
        ReadOnlySpan<char> value = _lexer.Value;
        if (value.SequenceEqual(nullPublicKeyToken))
        {
            publicKeyToken = null;
        }
        else
        {
            if (value.Length != 16)
                throw new TypeSyntaxParseException();

            publicKeyToken = 0;
            for (int i = 0; i < 16; i += 2)
            {
                if (!byte.TryParse(value.Slice(i, 2), NumberStyles.HexNumber, null, out byte b))
                    throw new TypeSyntaxParseException();

                publicKeyToken = (publicKeyToken << 8) | b;
            }
        }
        
        Consume(TypeSyntaxTokenKind.Equal);
        Consume(TypeSyntaxTokenKind.Word);
        
        if (!_lexer.ValueMatch("PublicKeyToken"))
            throw new TypeSyntaxParseException();
        
        return publicKeyToken;
    }

    private string AssemblyCulture()
    {
        Consume(TypeSyntaxTokenKind.Word);
        
        if (!TextFacts.IsValidCulture(_lexer.Value, out string? culture))
            throw new TypeSyntaxParseException();
        
        Consume(TypeSyntaxTokenKind.Equal);
        Consume(TypeSyntaxTokenKind.Word);

        if (!_lexer.ValueMatch("Culture"))
            throw new TypeSyntaxParseException();

        return culture;
    }

    private Version AssemblyVersion()
    {
        int revision = VersionNumber();
        
        Consume(TypeSyntaxTokenKind.Dot);
        
        int build = VersionNumber();
        
        Consume(TypeSyntaxTokenKind.Dot);
        
        int minor = VersionNumber();
        
        Consume(TypeSyntaxTokenKind.Dot);
        
        int major = VersionNumber();
        
        Consume(TypeSyntaxTokenKind.Equal);
        Consume(TypeSyntaxTokenKind.Word);
        
        if (!_lexer.ValueMatch("Version"))
            throw new TypeSyntaxParseException();

        return new Version(major, minor, build, revision);
    }

    private int VersionNumber()
    {
        Consume(TypeSyntaxTokenKind.Word);

        if (!int.TryParse(_lexer.Value, out int number))
            throw new TypeSyntaxParseException();
        
        return number;
    }

    private TypeFullName TypeFullName()
    {
        var typeName = TypeName();
        var @namespace = TryConsume(TypeSyntaxTokenKind.Dot)
            ? Name()
            : null;
        
        return new TypeFullName(@namespace, typeName);
    }

    private TypeNameSpec TypeName()
    {
        if (TryConsume(TypeSyntaxTokenKind.Ampersand))
            return new ByRefTypeNameSpec(NonReferenceTypeName());
        
        return NonReferenceTypeName();
    }

    private NonByRefTypeNameSpec NonReferenceTypeName()
    {
        if (TryConsume(TypeSyntaxTokenKind.Star))
            return new PointerTypeNameSpec(NonReferenceTypeName());

        return GenericOrArrayOrSimpleTypeName();
    }

    private NonByRefTypeNameSpec GenericOrArrayOrSimpleTypeName()
    {
        if (TryConsume(TypeSyntaxTokenKind.RightBracket))
        {
            return
                TryArrayTypeName(out var arrayTypeName) ? arrayTypeName :
                TryGenericTypeName(out var genericTypeName) ? genericTypeName :
                throw new TypeSyntaxParseException();
        }

        return TypeIdentifier();
    }

    private bool TryArrayTypeName([NotNullWhen(true)] out ArrayTypeNameSpec? spec)
    {
        int rank = 1;
        while (TryConsume(TypeSyntaxTokenKind.Comma))
        {
            rank++;
        }

        if (!TryConsume(TypeSyntaxTokenKind.LeftBracket))
        {
            if (rank != 1)
                throw new TypeSyntaxParseException();
            
            spec = null;
            return false;
        }

        var elementType = NonReferenceTypeName();
        
        spec = new ArrayTypeNameSpec(elementType, rank);
        return true;
    }

    private bool TryGenericTypeName([NotNullWhen(true)] out GenericTypeNameSpec? spec)
    {
        var genericArguments = ImmutableArray.CreateBuilder<AssemblyQualifiedName>();

        if (!TryConsume(TypeSyntaxTokenKind.RightBracket))
        {
            spec = null;
            return false;
        }
        
        genericArguments.Add(AssemblyQualifiedName());
        Consume(TypeSyntaxTokenKind.LeftBracket);
        
        while (TryConsume(TypeSyntaxTokenKind.Comma))
        {
            Consume(TypeSyntaxTokenKind.RightBracket);
            genericArguments.Add(AssemblyQualifiedName());
            Consume(TypeSyntaxTokenKind.LeftBracket);
        }

        Consume(TypeSyntaxTokenKind.LeftBracket);

        var definition = TypeIdentifier();
        spec = new GenericTypeNameSpec(definition, genericArguments.ToImmutable());
        return true;
    }

    private TypeIdentifierSpec TypeIdentifier()
    {
        var right = SimpleTypeIdentifier();

        if (!TryConsume(TypeSyntaxTokenKind.Plus))
            return right;

        var left = TypeIdentifier();
        return new NestedTypeIdentifierSpec(left, right);
    }

    private SimpleTypeIdentifierSpec SimpleTypeIdentifier()
    {
        Consume(TypeSyntaxTokenKind.Word);

        ReadOnlySpan<char> arityOrIdentifier = _lexer.Value;
        if (int.TryParse(arityOrIdentifier, out int arity))
        {
            if (arityOrIdentifier[0] == '0')
                throw new TypeSyntaxParseException();
            
            Consume(TypeSyntaxTokenKind.Backtick);
            Consume(TypeSyntaxTokenKind.Word);
            
            arityOrIdentifier = _lexer.Value;
        }
        
        return new SimpleTypeIdentifierSpec(arityOrIdentifier.ToString(), arity);
    }

    private NameSpec Name()
    {
        string identifier = Identifier();
        
        if (!TryConsume(TypeSyntaxTokenKind.Dot))
            return new SimpleNameSpec(identifier);

        var left = Name();
        return new QualifiedNameSpec(
            left,
            identifier);
    }

    private string Identifier()
    {
        Consume(TypeSyntaxTokenKind.Word);

        return _lexer.Value.ToString();
    }

    private void Consume(TypeSyntaxTokenKind kind)
    {
        if (!_lexer.MoveNext() || _lexer.Kind != kind)
            throw new TypeSyntaxParseException();
    }

    private bool TryConsume(TypeSyntaxTokenKind kind)
    {
        TypeSyntaxLexer lexer = _lexer;
        if (lexer.MoveNext() && lexer.Kind == kind)
        {
            _lexer = lexer;
            return true;
        }
        
        return false;
    }
    
    private sealed class TypeSyntaxParseException : Exception;
}