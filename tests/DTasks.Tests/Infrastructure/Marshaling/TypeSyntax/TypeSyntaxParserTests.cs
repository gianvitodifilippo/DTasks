namespace DTasks.Infrastructure.Marshaling.TypeSyntax;

public class TypeSyntaxParserTests
{
#if NET8_0
    private static readonly Version s_dotNetVersion = new Version(8, 0, 0, 0);
#elif NET9_0
    private static readonly Version s_dotNetVersion = new Version(9, 0, 0, 0);
#endif

    private static readonly AssemblyQualifiedName s_objectAssemblyQualifiedName = new AssemblyQualifiedName(
        new TypeFullName(
            new SimpleNameSpec("System"),
            new SimpleTypeIdentifierSpec("Object", 0)),
        new AssemblyFullName(
            new QualifiedNameSpec(
                new QualifiedNameSpec(
                    new SimpleNameSpec("System"),
                    "Private"),
                "CoreLib"),
            s_dotNetVersion,
            "neutral",
            0x7cec85d7bea7798e));

    private static readonly AssemblyQualifiedName s_int64AssemblyQualifiedName = new AssemblyQualifiedName(
        new TypeFullName(
            new SimpleNameSpec("System"),
            new SimpleTypeIdentifierSpec("Int64", 0)),
        new AssemblyFullName(
            new QualifiedNameSpec(
                new QualifiedNameSpec(
                    new SimpleNameSpec("System"),
                    "Private"),
                "CoreLib"),
            s_dotNetVersion,
            "neutral",
            0x7cec85d7bea7798e));
    
    [Fact]
    public void ParsesSimpleType()
    {
        // Arrange
        Type type = typeof(object);
        TypeSyntaxParser sut = new(type.AssemblyQualifiedName!);

        // Act
        AssemblyQualifiedName result = sut.Parse();

        // Assert
        result.Should().BeEquivalentTo(s_objectAssemblyQualifiedName);
    }
    
    [Fact]
    public void ParsesByRefType()
    {
        // Arrange
        Type type = typeof(object).MakeByRefType();
        TypeSyntaxParser sut = new(type.AssemblyQualifiedName!);

        // Act
        AssemblyQualifiedName result = sut.Parse();

        // Assert
        result.Should().BeEquivalentTo(new AssemblyQualifiedName(
            new TypeFullName(
                new SimpleNameSpec("System"),
                new ByRefTypeNameSpec(
                    new SimpleTypeIdentifierSpec("Object", 0))),
            new AssemblyFullName(
                new QualifiedNameSpec(
                    new QualifiedNameSpec(
                        new SimpleNameSpec("System"),
                        "Private"),
                    "CoreLib"),
                s_dotNetVersion,
                "neutral",
                0x7cec85d7bea7798e)));
    }
    
    [Fact]
    public void ParsesPointerType()
    {
        // Arrange
        Type type = typeof(object).MakePointerType();
        TypeSyntaxParser sut = new(type.AssemblyQualifiedName!);

        // Act
        AssemblyQualifiedName result = sut.Parse();

        // Assert
        result.Should().BeEquivalentTo(new AssemblyQualifiedName(
            new TypeFullName(
                new SimpleNameSpec("System"),
                new PointerTypeNameSpec(
                    new SimpleTypeIdentifierSpec("Object", 0))),
            new AssemblyFullName(
                new QualifiedNameSpec(
                    new QualifiedNameSpec(
                        new SimpleNameSpec("System"),
                        "Private"),
                    "CoreLib"),
                s_dotNetVersion,
                "neutral",
                0x7cec85d7bea7798e)));
    }
    
    [Fact]
    public void ParsesArrayType()
    {
        // Arrange
        Type type = typeof(object).MakeArrayType();
        TypeSyntaxParser sut = new(type.AssemblyQualifiedName!);

        // Act
        AssemblyQualifiedName result = sut.Parse();

        // Assert
        result.Should().BeEquivalentTo(new AssemblyQualifiedName(
            new TypeFullName(
                new SimpleNameSpec("System"),
                new ArrayTypeNameSpec(
                    new SimpleTypeIdentifierSpec("Object", 0),
                    1)),
            new AssemblyFullName(
                new QualifiedNameSpec(
                    new QualifiedNameSpec(
                        new SimpleNameSpec("System"),
                        "Private"),
                    "CoreLib"),
                s_dotNetVersion,
                "neutral",
                0x7cec85d7bea7798e)));
    }
    
    [Fact]
    public void ParsesMDArrayType()
    {
        // Arrange
        Type type = typeof(object).MakeArrayType(3);
        TypeSyntaxParser sut = new(type.AssemblyQualifiedName!);

        // Act
        AssemblyQualifiedName result = sut.Parse();

        // Assert
        result.Should().BeEquivalentTo(new AssemblyQualifiedName(
            new TypeFullName(
                new SimpleNameSpec("System"),
                new ArrayTypeNameSpec(
                    new SimpleTypeIdentifierSpec("Object", 0),
                    3)),
            new AssemblyFullName(
                new QualifiedNameSpec(
                    new QualifiedNameSpec(
                        new SimpleNameSpec("System"),
                        "Private"),
                    "CoreLib"),
                s_dotNetVersion,
                "neutral",
                0x7cec85d7bea7798e)));
    }
    
    [Fact]
    public void ParsesGenericType()
    {
        // Arrange
        Type type = typeof(Dictionary<object, int>);
        TypeSyntaxParser sut = new(type.AssemblyQualifiedName!);

        // Act
        AssemblyQualifiedName result = sut.Parse();

        // Assert
        result.Should().BeEquivalentTo(new AssemblyQualifiedName(
            new TypeFullName(
                new QualifiedNameSpec(
                    new QualifiedNameSpec(
                        new SimpleNameSpec("System"),
                        "Collections"),
                    "Generic"),
                new GenericTypeNameSpec(
                    new SimpleTypeIdentifierSpec("Dictionary", 2), [
                        s_objectAssemblyQualifiedName,
                        s_int64AssemblyQualifiedName
                    ])),
            new AssemblyFullName(
                new QualifiedNameSpec(
                    new QualifiedNameSpec(
                        new SimpleNameSpec("System"),
                        "Private"),
                    "CoreLib"),
                s_dotNetVersion,
                "neutral",
                0x7cec85d7bea7798e)));
    }
}