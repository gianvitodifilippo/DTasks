using DTasks.Utils;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DTasks.Hosting;

public readonly struct DAsyncId : IEquatable<DAsyncId>
{
    private const int ByteCount = 3 * sizeof(uint);

    internal static readonly DAsyncId RootId = new(uint.MaxValue, uint.MaxValue, uint.MaxValue);

#if !NET6_0_OR_GREATER
    private static readonly ThreadLocal<Random> s_randomLocal = new(static () => new Random());
#endif

    private readonly uint _a;
    private readonly uint _b;
    private readonly uint _c;

    private DAsyncId(uint a, uint b, uint c)
    {
        _a = a;
        _b = b;
        _c = c;
    }

    private DAsyncId(ReadOnlySpan<byte> bytes)
    {
        Debug.Assert(bytes.Length == ByteCount);

        this = Unsafe.ReadUnaligned<DAsyncId>(ref MemoryMarshal.GetReference(bytes));
    }

    internal bool IsDefault => this == default;

    internal bool IsRoot => this == RootId;

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

    public bool Equals(DAsyncId other) =>
        _a == other._a &&
        _b == other._b &&
        _c == other._c;

    public override bool Equals(object? obj) => obj is DAsyncId other && Equals(other);

    public override int GetHashCode()
    {
        ref int head = ref Unsafe.As<DAsyncId, int>(ref Unsafe.AsRef(in this));
        return head ^ Unsafe.Add(ref head, 1) ^ Unsafe.Add(ref head, 2);
    }

    public override string ToString()
    {
        ref byte head = ref Unsafe.As<DAsyncId, byte>(ref Unsafe.AsRef(in this));
        ReadOnlySpan<byte> bytes = MemoryMarshal.CreateReadOnlySpan(ref head, ByteCount);

        return Convert.ToBase64String(bytes);
    }

    public static bool operator ==(DAsyncId left, DAsyncId right) => left.Equals(right);

    public static bool operator !=(DAsyncId left, DAsyncId right) => !left.Equals(right);

    internal static DAsyncId New()
    {
        Span<byte> bytes = stackalloc byte[ByteCount];
        DAsyncId id;

        do
        {
            Randomize(bytes);
            id = new DAsyncId(bytes);
        }
        while (id == default || id == RootId);

        return id;
    }

    private static void Randomize(Span<byte> bytes)
    {
#if NET6_0_OR_GREATER
        Random.Shared.NextBytes(bytes);
#else
        s_randomLocal.Value.NextBytes(bytes);
#endif
    }

    public static DAsyncId Parse(string value)
    {
        ThrowHelper.ThrowIfNull(value);

        if (!TryParseCore(value, out DAsyncId id))
            throw new ArgumentException($"'{value}' does not represent a valid DAsyncId.", nameof(value));

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
            throw new ArgumentException("The provided bytes do not represent a valid DAsyncId.", nameof(bytes));

        return id;
    }

    public static bool TryReadBytes(ReadOnlySpan<byte> bytes, out DAsyncId id)
    {
        return TryReadBytesCore(bytes, out id);
    }

    private static bool TryParseCore(string value, out DAsyncId id)
    {
        Span<byte> bytes = stackalloc byte[ByteCount];
        if (!Convert.TryFromBase64String(value, bytes, out int bytesWritten) || bytesWritten != ByteCount)
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
}
