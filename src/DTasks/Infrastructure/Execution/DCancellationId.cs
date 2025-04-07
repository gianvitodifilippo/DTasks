using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DTasks.Utils;

namespace DTasks.Infrastructure.Execution;

[EditorBrowsable(EditorBrowsableState.Never)]
[StructLayout(LayoutKind.Sequential)]
public readonly struct DCancellationId : IEquatable<DCancellationId>
{
    private const int ByteCount = 2 * sizeof(uint);

#if DEBUG_TESTS
    private static int s_idCount = 0;
#elif !NET6_0_OR_GREATER
    private static readonly ThreadLocal<Random> s_randomLocal = new(static () => new Random());
#endif

    private readonly uint _a;
    private readonly uint _b;

    private DCancellationId(uint a, uint b)
    {
        _a = a;
        _b = b;
    }

    private DCancellationId(ReadOnlySpan<byte> bytes)
    {
        Debug.Assert(bytes.Length == ByteCount);

        this = Unsafe.ReadUnaligned<DCancellationId>(ref MemoryMarshal.GetReference(bytes));
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

    public bool Equals(DCancellationId other) =>
        _a == other._a &&
        _b == other._b;

    public override bool Equals(object? obj) => obj is DCancellationId other && Equals(other);

    public override int GetHashCode()
    {
        ref int head = ref Unsafe.As<DCancellationId, int>(ref Unsafe.AsRef(in this));
        return head ^ Unsafe.Add(ref head, 1) ^ Unsafe.Add(ref head, 2);
    }

    public override string ToString()
    {
        if (this == default)
            return "<default>";

        ref byte head = ref Unsafe.As<DCancellationId, byte>(ref Unsafe.AsRef(in this));
        ReadOnlySpan<byte> bytes = MemoryMarshal.CreateReadOnlySpan(ref head, ByteCount);

        return Convert.ToBase64String(bytes);
    }

    public static bool operator ==(DCancellationId left, DCancellationId right) => left.Equals(right);

    public static bool operator !=(DCancellationId left, DCancellationId right) => !left.Equals(right);

    internal static DCancellationId New()
    {
        Span<byte> bytes = stackalloc byte[ByteCount];
        DCancellationId id;

        do
        {
            Randomize(bytes);
            id = new DCancellationId(bytes);
        }
        while (id == default);

        return id;
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

    public static DCancellationId Parse(string value)
    {
        ThrowHelper.ThrowIfNull(value);

        if (!TryParseCore(value, out DCancellationId id))
            throw new ArgumentException($"'{value}' does not represent a valid {nameof(DCancellationId)}.", nameof(value));

        return id;
    }

    public static bool TryParse(string value, out DCancellationId id)
    {
        ThrowHelper.ThrowIfNull(value);

        return TryParseCore(value, out id);
    }

    public static DCancellationId ReadBytes(ReadOnlySpan<byte> bytes)
    {
        if (!TryReadBytesCore(bytes, out DCancellationId id))
            throw new ArgumentException($"The provided bytes do not represent a valid {nameof(DCancellationId)}.", nameof(bytes));

        return id;
    }

    public static bool TryReadBytes(ReadOnlySpan<byte> bytes, out DCancellationId id)
    {
        return TryReadBytesCore(bytes, out id);
    }

    private static bool TryParseCore(string value, out DCancellationId id)
    {
        Span<byte> bytes = stackalloc byte[ByteCount];
        if (!Convert.TryFromBase64String(value, bytes, out int bytesWritten) || bytesWritten != ByteCount)
        {
            id = default;
            return false;
        }

        return TryReadBytesCore(bytes, out id);
    }

    private static bool TryReadBytesCore(ReadOnlySpan<byte> bytes, out DCancellationId id)
    {
        if (bytes.Length != ByteCount)
        {
            id = default;
            return false;
        }

        id = new DCancellationId(bytes);
        return true;
    }
}