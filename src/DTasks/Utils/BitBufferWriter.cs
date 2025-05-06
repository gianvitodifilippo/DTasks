using System.Diagnostics;

namespace DTasks.Utils;

internal sealed class BitBufferWriter : IBitBufferWriter
{
    private const int BitsPerByte = 8;
    
    private byte[] _buffer;
    private int _writtenCount;

    public BitBufferWriter()
    {
        _buffer = new byte[1];
    }

    public BitBufferWriter(int initialCapacity)
    {
        _buffer = new byte[initialCapacity];
    }

    public int Length => _writtenCount;

    public bool this[int index]
    {
        get
        {
            int byteIndex = index / BitsPerByte;
            int shift = BitsPerByte - index % BitsPerByte - 1;
            
            byte b = _buffer[byteIndex];
            return ((b >> shift) & 1) == 1;
        }
    }

    public byte[] ToArray()
    {
        int byteCount = (_writtenCount + BitsPerByte - 1) / BitsPerByte;
        byte[] result = new byte[byteCount];
        _buffer.AsSpan(0, byteCount).CopyTo(result);

        return result;
    }
    
    public void WriteBits(int value, int bitCount)
    {
        Debug.Assert(bitCount is > 0 and <= 32);
        Debug.Assert(value >= 0 && value < 1 << bitCount);
        
        int byteIndex = _writtenCount / BitsPerByte;
        int shift = bitCount + _writtenCount % BitsPerByte - BitsPerByte;

        _writtenCount += bitCount;
        EnsureCapacity();

        while (shift > 0)
        {
            _buffer[byteIndex] |= (byte)((value >> shift) & byte.MaxValue);
            shift -= BitsPerByte;
            byteIndex++;
        }

        _buffer[byteIndex] |= (byte)((value << -shift) & byte.MaxValue);
    }

    private void EnsureCapacity()
    {
        if (_buffer.Length * BitsPerByte >= _writtenCount)
            return;

        int newCapacity = Math.Max(_writtenCount / BitsPerByte + 1, _buffer.Length * 2);
        byte[] newBuffer = new byte[newCapacity];
        _buffer.CopyTo(newBuffer, 0);
        _buffer = newBuffer;
    }
}