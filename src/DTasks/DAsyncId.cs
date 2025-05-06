using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DTasks.Utils;

namespace DTasks;

[StructLayout(LayoutKind.Sequential)]
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly struct DAsyncId : IEquatable<DAsyncId>
{
    private const int ByteCount = 3 * sizeof(uint);
    private const int CharCount = ByteCount * 8 / 6;
    private const byte ReservedBitsMask = 0b_11110000;
    private const byte ReservedBitsInvertedMask = ~ReservedBitsMask & byte.MaxValue;
    private const byte FlowIdMask = 0b_10000000;

#if DEBUG_TESTS
    private static int s_idCount = 0;
#elif !NET6_0_OR_GREATER
    private static readonly ThreadLocal<Random> s_randomLocal = new(static () => new Random());
#endif

    private readonly uint _a;
    private readonly uint _b;
    private readonly uint _c;

    private DAsyncId(ReadOnlySpan<byte> bytes)
    {
        Debug.Assert(bytes.Length == ByteCount);

        this = Unsafe.ReadUnaligned<DAsyncId>(ref MemoryMarshal.GetReference(bytes));
    }

    private ReadOnlySpan<byte> Bytes
    {
        get
        {
            ref byte head = ref Unsafe.As<DAsyncId, byte>(ref Unsafe.AsRef(in this));
            return MemoryMarshal.CreateReadOnlySpan(ref head, ByteCount);
        }
    }

    internal bool IsDefault => this == default;

    internal bool IsFlowId
    {
        get
        {
            ref readonly byte firstByte = ref Unsafe.As<DAsyncId, byte>(ref Unsafe.AsRef(in this));
            return (firstByte & FlowIdMask) != 0;
        }
    }

    public byte[] ToByteArray()
    {
        byte[] bytes = new byte[ByteCount];
        Unsafe.WriteUnaligned(ref bytes[0], this);

        return bytes;
    }

    public bool TryWriteBytes(Span<byte> destination)
    {
        if (ByteCount > destination.Length)
            return false;

        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), this);
        return true;
    }

    public bool TryWriteChars(Span<char> destination)
    {
        if (CharCount > destination.Length)
            return false;

        bool result = Convert.TryToBase64Chars(Bytes, destination, out int charsWritten);
        
        Debug.Assert(result);
        Debug.Assert(charsWritten == CharCount);

        return result;
    }

    public bool Equals(DAsyncId other) =>
        _a == other._a &&
        _b == other._b &&
        _c == other._c;

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is DAsyncId other && Equals(other);

    public override int GetHashCode()
    {
        ref int head = ref Unsafe.As<DAsyncId, int>(ref Unsafe.AsRef(in this));
        return head ^ Unsafe.Add(ref head, 1) ^ Unsafe.Add(ref head, 2);
    }

    public override string ToString()
    {
        return Convert.ToBase64String(Bytes);
    }

    public static bool operator ==(DAsyncId left, DAsyncId right) => left.Equals(right);

    public static bool operator !=(DAsyncId left, DAsyncId right) => !left.Equals(right);

    internal static DAsyncId New()
    {
        Span<byte> bytes = stackalloc byte[ByteCount];

        Create(bytes);
        return new(bytes);
    }

    internal static DAsyncId NewFlowId()
    {
        Span<byte> bytes = stackalloc byte[ByteCount];

        Create(bytes);
        bytes[0] |= FlowIdMask;
        return new(bytes);
    }

    private static void Create(Span<byte> bytes)
    {
        do
        {
            Randomize(bytes);
        }
        while (IsDefault(bytes));

        bytes[0] &= ReservedBitsInvertedMask;

        static bool IsDefault(Span<byte> bytes)
        {
            foreach (byte b in bytes)
            {
                if (b != 0)
                    return false;
            }

            return true;
        }
    }

    private static void Randomize(Span<byte> bytes)
    {
#if DEBUG_TESTS
        s_idCount++;
        string stringId = s_idCount.ToString("0000000000000000");
        bool hasConverted = Convert.TryFromBase64String(stringId, bytes, out int bytesWritten);
        Debug.Assert(hasConverted && bytesWritten == ByteCount);
#elif NET6_0_OR_GREATER
        Random.Shared.NextBytes(bytes);
#else
        s_randomLocal.Value.NextBytes(bytes);
#endif
    }

    public static DAsyncId Parse(string value)
    {
        ThrowHelper.ThrowIfNull(value);

        if (!TryParseCore(value, out DAsyncId id))
            throw new ArgumentException($"'{value}' does not represent a valid {nameof(DAsyncId)}.", nameof(value));

        return id;
    }

    public static bool TryParse(ReadOnlySpan<char> value, out DAsyncId id)
    {
        return TryParseCore(value, out id);
    }

    public static DAsyncId Parse(ReadOnlySpan<char> value)
    {
        if (!TryParseCore(value, out DAsyncId id))
            throw new ArgumentException($"'{value.ToString()}' does not represent a valid {nameof(DAsyncId)}.", nameof(value));

        return id;
    }

    public static bool TryParse(string value, out DAsyncId id)
    {
        ThrowHelper.ThrowIfNull(value);

        return TryParseCore(value, out id);
    }

    public static DAsyncId ReadBytes(ReadOnlySpan<byte> bytes)
    {
        if (!TryReadBytesCore(bytes, out DAsyncId id))
            throw new ArgumentException($"The provided bytes do not represent a valid {nameof(DAsyncId)}.", nameof(bytes));

        return id;
    }

    public static bool TryReadBytes(ReadOnlySpan<byte> bytes, out DAsyncId id)
    {
        return TryReadBytesCore(bytes, out id);
    }

    private static bool TryParseCore(ReadOnlySpan<char> value, out DAsyncId id)
    {
        Span<byte> bytes = stackalloc byte[ByteCount];
        if (!Convert.TryFromBase64Chars(value, bytes, out int bytesWritten) || bytesWritten != ByteCount)
        {
            id = default;
            return false;
        }

        return TryReadBytesCore(bytes, out id);
    }

    private static bool TryReadBytesCore(ReadOnlySpan<byte> bytes, out DAsyncId id)
    {
        if (bytes.Length != ByteCount)
        {
            id = default;
            return false;
        }

        id = new DAsyncId(bytes);
        return true;
    }

    private string DebuggerDisplay
    {
        get
        {
            if (IsDefault)
                return "<default>";
#if DEBUG_TESTS
            string id = ToString()[^4..];
            return IsFlowId ? $"root:{id}" : id;
#else
            return ToString();
#endif
        }
    }
}
