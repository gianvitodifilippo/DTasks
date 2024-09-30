using DTasks.Utils;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DTasks.Hosting;

public readonly struct FlowId : IEquatable<FlowId>
{
#if !NET8_0_OR_GREATER
    private static readonly ThreadLocal<Random> s_randomLocal = new(static () => new Random());
#endif

    private static readonly byte s_mainIndex        = 0b1111_1111;
    private static readonly byte s_kindMask         = 0b0000_0111;
    private static readonly byte s_kindInverseMask  = 0b1111_1000;
    private static readonly byte s_stateMask        = 0b0000_1000;
    private static readonly byte s_stateInverseMask = 0b1111_0111;

    private readonly byte _b0; // Aggregate index
    private readonly byte _b1;
    private readonly byte _b2;
    private readonly byte _b3;
    private readonly byte _b4;
    private readonly byte _b5;
    private readonly byte _b6;
    private readonly byte _b7; // Kind and result flag

    private FlowId(byte b0, byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7)
    {
        _b0 = b0;
        _b1 = b1;
        _b2 = b2;
        _b3 = b3;
        _b4 = b4;
        _b5 = b5;
        _b6 = b6;
        _b7 = b7;
    }

    private FlowId(ReadOnlySpan<byte> bytes)
    {
        _b0 = bytes[0];
        _b1 = bytes[1];
        _b2 = bytes[2];
        _b3 = bytes[3];
        _b4 = bytes[4];
        _b5 = bytes[5];
        _b6 = bytes[6];
        _b7 = bytes[7];
    }

    public FlowKind Kind => (FlowKind)(_b7 & s_kindMask);

    internal bool IsStateful => (_b7 & s_stateMask) != 0;

    internal bool IsMainId
    {
        get
        {
            Debug.Assert(Kind.IsAggregate());

            return _b0 == s_mainIndex;
        }
    }

    internal byte BranchIndex
    {
        get
        {
            Debug.Assert(!IsMainId);
            return _b0;
        }
    }

    internal FlowId GetMainId()
    {
        Debug.Assert(!IsMainId);

        return new(s_mainIndex, _b1, _b2, _b3, _b4, _b5, _b6, _b7);
    }

    internal FlowId GetBranchId(byte index, bool isStateful)
    {
        Debug.Assert(IsMainId);

        byte b7 = _b7;
        SetResultFlag(ref b7, isStateful);

        return new(index, _b1, _b2, _b3, _b4, _b5, _b6, b7);
    }

    public byte[] ToByteArray()
    {
        byte[] bytes = new byte[8];

#if NET8_0_OR_GREATER
        MemoryMarshal.Write(bytes, in this);
#else
        MemoryMarshal.Write(bytes, ref Unsafe.AsRef(in this));
#endif

        return bytes;
    }

    public bool TryWriteBytes(Span<byte> destination)
    {
#if NET8_0_OR_GREATER
        return MemoryMarshal.TryWrite(destination, in this);
#else
        return MemoryMarshal.TryWrite(destination, ref Unsafe.AsRef(in this));
#endif
    }

    public bool Equals(FlowId other)
    {
        ref ulong head1 = ref Unsafe.As<FlowId, ulong>(ref Unsafe.AsRef(in this));
        ref ulong head2 = ref Unsafe.As<FlowId, ulong>(ref other);

        return head1 == head2;
    }

    public override bool Equals(object? obj) => obj is FlowId other && Equals(other);

    public override int GetHashCode()
    {
        ref int head = ref Unsafe.As<FlowId, int>(ref Unsafe.AsRef(in this));
        return head ^ Unsafe.Add(ref head, 1);
    }

    public override string ToString()
    {
        ref byte head = ref Unsafe.As<FlowId, byte>(ref Unsafe.AsRef(in this));
        ReadOnlySpan<byte> bytes = MemoryMarshal.CreateReadOnlySpan(ref head, 8);

        return Convert.ToBase64String(bytes);
    }

    public static bool operator ==(FlowId left, FlowId right) => left.Equals(right);

    public static bool operator !=(FlowId left, FlowId right) => !(left == right);

    public static FlowId New(FlowKind kind)
    {
        return kind switch
        {
            FlowKind.Hosted => NewHosted(isStateful: true),
            FlowKind.WhenAll or FlowKind.WhenAny => NewAggregate(kind),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), "Invalid flow kind.")
        };
    }

    internal static FlowId NewHosted(bool isStateful)
    {
        Span<byte> bytes = stackalloc byte[8];
        Randomize(bytes);
        SetKind(ref bytes[7], FlowKind.Hosted);
        SetResultFlag(ref bytes[7], isStateful);
        // bytes[0] = 0; // Should we?

        return new FlowId(bytes);
    }

    internal static FlowId NewAggregate(FlowKind kind)
    {
        Debug.Assert(kind.IsAggregate(), "Expected an aggregate flow kind.");

        Span<byte> bytes = stackalloc byte[8];
        Randomize(bytes);
        SetKind(ref bytes[7], kind);
        SetResultFlag(ref bytes[7], false);
        bytes[0] = s_mainIndex;

        return new FlowId(bytes);
    }

    private static void Randomize(Span<byte> bytes)
    {
#if NET8_0_OR_GREATER
        Random.Shared.NextBytes(bytes);
#else
        s_randomLocal.Value.NextBytes(bytes);
#endif
    }

    private static void SetKind(ref byte b7, FlowKind kind)
    {
        Debug.Assert((byte)kind < s_kindMask, "The value of the flow kind must fit into 3 bits.");

        b7 &= s_kindInverseMask;
        b7 |= (byte)kind;
    }

    private static void SetResultFlag(ref byte b7, bool isStateful)
    {
        if (isStateful)
        {
            b7 |= s_stateMask;
        }
        else
        {
            b7 &= s_stateInverseMask;
        }
    }

    public static bool TryParse(string value, out FlowId id)
    {
        ThrowHelper.ThrowIfNull(value);

        Span<byte> bytes = stackalloc byte[8];
        if (!Convert.TryFromBase64String(value, bytes, out int bytesWritten) || bytesWritten != 8)
        {
            id = default;
            return false;
        }

        return TryCreateCore(bytes, out id);
    }

    public static bool TryReadBytes(ReadOnlySpan<byte> bytes, out FlowId id)
    {
        return TryCreateCore(bytes, out id);
    }

    private static bool TryCreateCore(ReadOnlySpan<byte> bytes, out FlowId id)
    {
        if (!IsValidKind(bytes[7]))
        {
            id = default;
            return false;
        }

        id = new(bytes);
        return true;
    }

    private static bool IsValidKind(byte b7)
    {
        b7 &= s_kindMask;
        return b7 is >= (byte)FlowKind.Hosted and <= (byte)FlowKind.WhenAny;
    }
}
