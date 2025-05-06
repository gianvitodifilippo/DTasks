namespace DTasks.Utils;

internal interface IBitBufferWriter
{
    void WriteBits(int value, int bitCount);
}

internal static class BitBufferWriterExtensions
{
    public static void WriteInt32(this IBitBufferWriter bufferWriter, int value)
    {
        bufferWriter.WriteBits(value, 32);
    }

    public static void WriteChar(this IBitBufferWriter bufferWriter, char value)
    {
        bufferWriter.WriteBits(value, 16);
    }

    public static void WriteByte(this IBitBufferWriter bufferWriter, byte value)
    {
        bufferWriter.WriteBits(value, 8);
    }

    public static void Write7Bits(this IBitBufferWriter bufferWriter, int value)
    {
        bufferWriter.WriteBits(value, 7);
    }

    public static void Write6Bits(this IBitBufferWriter bufferWriter, int value)
    {
        bufferWriter.WriteBits(value, 6);
    }

    public static void Write5Bits(this IBitBufferWriter bufferWriter, int value)
    {
        bufferWriter.WriteBits(value, 5);
    }
    
    public static void Write4Bits(this IBitBufferWriter bufferWriter, int value)
    {
        bufferWriter.WriteBits(value, 4);
    }
    
    public static void Write3Bits(this IBitBufferWriter bufferWriter, int value)
    {
        bufferWriter.WriteBits(value, 3);
    }
    
    public static void Write2Bits(this IBitBufferWriter bufferWriter, int value)
    {
        bufferWriter.WriteBits(value, 2);
    }
    
    public static void Write1Bit(this IBitBufferWriter bufferWriter, int value)
    {
        bufferWriter.WriteBits(value, 1);
    }
}