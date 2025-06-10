using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DTasks.Infrastructure.Marshaling.Encoding;
using DTasks.Infrastructure.Marshaling.TypeSyntax;
using DTasks.Utils;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct TypeId : IEquatable<TypeId>
{
    private readonly string _value;

    private TypeId(string value) => _value = value;

    public bool Equals(TypeId other) => _value == other._value;

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is TypeId other && Equals(other);

    public override int GetHashCode() => _value?.GetHashCode() ?? typeof(TypeId).GetHashCode();

    public override string ToString() => _value;

    public static bool operator ==(TypeId left, TypeId right) => left.Equals(right);

    public static bool operator !=(TypeId left, TypeId right) => !left.Equals(right);

    public static bool TryParse(string value, out TypeId typeId)
    {
        ThrowHelper.ThrowIfNull(value);
        
        return TryParseCore(value, out typeId);
    }

    public static TypeId Parse(string value)
    {
        ThrowHelper.ThrowIfNull(value);
        
        if (!TryParseCore(value, out TypeId typeId))
            throw new FormatException("Invalid type identifier.");
        
        return typeId;
    }

    private static bool TryParseCore(string value, out TypeId typeId)
    {
        if (value[0] is >= 'E' and <= 'L')
        {
            typeId = new(value);
            return true;
        }
        
        typeId = default;
        return false;
    }

    public static TypeId FromEncodedTypeName(Type type, TypeEncodingStrategy encodingStrategy = TypeEncodingStrategy.FullName)
    {
        ThrowHelper.ThrowIfNull(type);
        
        if (type.ContainsGenericParameters)
            throw new ArgumentException("Open generic types are not supported.", nameof(type));

        string? assemblyQualifiedName = type.AssemblyQualifiedName;
        if (assemblyQualifiedName is null)
            throw new ArgumentException("The provided type did not have an assembly qualified name.", nameof(type));
        
        int initialCapacity = encodingStrategy switch
        {
            TypeEncodingStrategy.AssemblyQualifiedName => 16,
            TypeEncodingStrategy.FullName => 8,
            TypeEncodingStrategy.Name => 2,
            _ => throw new ArgumentOutOfRangeException(nameof(encodingStrategy), encodingStrategy, null)
        };
        
        BitBufferWriter bufferWriter = new(initialCapacity);
        bufferWriter.Write4Bits((int)TypeIdKind.EncodedType);
        
        var name = AssemblyQualifiedName.Parse(assemblyQualifiedName);
        AssemblyQualifiedNameEncoder.Encode(bufferWriter, name, encodingStrategy);
        
        return Create(bufferWriter);
    }
    
    public static TypeId FromConstant(string value)
    {
        ThrowHelper.ThrowIfNull(value);
        
        byte[] encodedBytes = System.Text.Encoding.UTF8.GetBytes(value);
        
        BitBufferWriter bufferWriter = new(encodedBytes.Length + 1);
        bufferWriter.Write4Bits((int)TypeIdKind.Constant);

        foreach (byte encodedByte in encodedBytes)
        {
            bufferWriter.WriteByte(encodedByte);
        }
        
        return Create(bufferWriter);
    }

    private static TypeId Create(BitBufferWriter bufferWriter)
    {
        byte[] bytes = bufferWriter.ToArray();
        string value = Convert.ToBase64String(bytes);

        return new(value);
    }
}
