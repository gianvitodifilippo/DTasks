using DTasks.Infrastructure.Marshaling.TypeSyntax;
using DTasks.Utils;

namespace DTasks.Infrastructure.Marshaling.Encoding;

public class AssemblyQualifiedNameEncoderTests
{
    [Fact]
    public void EncodeAssemblyQualifiedName()
    {
        // Arrange
        BitBufferWriter bufferWriter = new();
        
        var name = new AssemblyQualifiedName(
            new TypeFullName(
                new QualifiedNameSpec(
                    new SimpleNameSpec("Some"),
                    "Namespace"),
                new SimpleTypeIdentifierSpec("MyClass", 0)),
            new AssemblyFullName(
                new SimpleNameSpec("Some"),
                new Version(1, 2, 3, 4),
                "neutral",
                null));
        
        string expectedResult =
            "10010011100110000100" + // Some
            "11110011" + // QUALIFIED_NAME_SEPARATOR
            "011010000001100001001001001111000000001000100" + // Namespace
            "11110011" + // QUALIFIED_NAME_SEPARATOR
            "01100110001101101001011000001001010010" + // MyClass
            "111111111" + // ASSEMBLY_QUALIFIED_NAME_SEPARATOR
            "10010011100110000100" + // SOME
            "111111100" + // START_ASSEMBLY_PROPERTIES
            "00001000100001100100" + // VERSION
            "0" + // CULTURE
            "0"; // PUBLICKEYTOKEN
        
        // Act
        AssemblyQualifiedNameEncoder.Encode(bufferWriter, name, TypeEncodingStrategy.AssemblyQualifiedName);

        // Assert
        Verify(bufferWriter, expectedResult);
    }

    private static void Verify(BitBufferWriter bufferWriter, string expectedResult)
    {
        bufferWriter.Length.Should().Be(expectedResult.Length);

        for (int i = 0; i < expectedResult.Length; i++)
        {
            bufferWriter[i].Should().Be(expectedResult[i] == '1');
        }
    }
}