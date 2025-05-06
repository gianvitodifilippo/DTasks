using System.Collections;
using System.Diagnostics;
using DTasks.Infrastructure.Marshaling.TypeSyntax;
using DTasks.Utils;

namespace DTasks.Infrastructure.Marshaling.Encoding;

internal struct AssemblyQualifiedNameWriter(IBitBufferWriter bufferWriter)
{
    public void WriteIdentifier(string identifier)
    {
        switch (identifier)
        {
            case "System":
                WriteCharCategory27(0);
                break;
            
            case "Collections":
                WriteCharCategory27(1);
                break;
            
            case "Generic":
                WriteCharCategory27(2);
                break;
            
            case "Immutable":
                WriteCharCategory27(3);
                break;
            
            case "Frozen":
                WriteCharCategory27(4);
                break;
        }
        
        WriteIdentifierStartChar(identifier[0]);
        for (int i = 1; i < identifier.Length; i++)
        {
            WriteIdentifierPartChar(identifier[i]);
        }
    }

    public void WriteQualifiedNameSeparator()
    {
        WriteCharCategory30(3);
    }

    public void WriteNestedTypeSeparator()
    {
        WriteCharCategory30(4);
    }

    public void WriteGenericTypeArity(int arity)
    {
        Debug.Assert(arity > 0);
        if (arity <= 8)
        {
            WriteCharCategory30(5);
            bufferWriter.Write3Bits(arity - 1);
        }
        else
        {
            WriteCharCategory30(6);
            bufferWriter.WriteInt32(arity);
        }
    }

    public void WriteGenericArgumentStart()
    {
        WriteCharCategory30(7);
    }

    public void WriteAssemblyPropertiesStart()
    {
        WriteCharCategory31(12);
    }

    public void WriteArrayRank(int rank)
    {
        Debug.Assert(rank > 0);
        
        if (rank == 1)
        {
            WriteCharCategory31(13);
        }
        else
        {
            WriteCharCategory31(14);
            bufferWriter.Write5Bits(rank);
        }
    }

    public void WriteAssemblyQualifiedNameSeparator()
    {
        WriteCharCategory31(15);
    }

    public void WriteVersion(Version version)
    {
        WriteVersionNumber(version.Major);
        WriteVersionNumber(version.Minor);
        WriteVersionNumber(version.Build);
        WriteVersionNumber(version.Revision);
    }

    public void WriteCulture(string culture)
    {
        if (culture == "neutral")
        {
            bufferWriter.Write1Bit(0);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public void WritePublicKeyToken(ulong? publicKeyToken)
    {
        bufferWriter.Write1Bit(publicKeyToken.HasValue ? 1 : 0);
    }

    private void WriteVersionNumber(int number)
    {
        if (number < 16)
        {
            bufferWriter.Write1Bit(0);
            bufferWriter.Write4Bits(number);
        }
        else
        {
            bufferWriter.Write1Bit(1);
            bufferWriter.WriteInt32(number);
        }
    }

    private void WriteIdentifierStartChar(char c)
    {
        switch (c)
        {
            case >= 'A' and <= 'Z':
                bufferWriter.Write5Bits(c - 'A');
                return;
            
            case >= 'a' and <= 'h':
                WriteCharCategory27(c - 'a');
                return;
            
            case >= 'i' and <= 'p':
                WriteCharCategory28(c - 'i');
                return;
            
            case >= 'q' and <= 'x':
                WriteCharCategory29(c - 'q');
                return;
            
            case 'y' or 'z':
                WriteCharCategory30(c - 'y');
                return;
            
            case '_':
                WriteCharCategory30(2);
                return;
        }
        
        WriteCharCategory0(c);
    }

    private void WriteIdentifierPartChar(char c)
    {
        switch (c)
        {
            case >= 'a' and <= 'z':
                bufferWriter.Write5Bits(c - 'a');
                return;
            
            case >= 'A' and <= 'H':
                WriteCharCategory27(c - 'A');
                return;
            
            case >= 'I' and <= 'P':
                WriteCharCategory28(c - 'I');
                return;
            
            case >= 'Q' and <= 'X':
                WriteCharCategory29(c - 'Q');
                return;
            
            case 'Y' or 'Z':
                WriteCharCategory30(c - 'Y');
                return;
            
            case '_':
                WriteCharCategory30(2);
                return;
            
            case >= '0' and <= '9':
                WriteCharCategory31(c - '0');
                return;
        }
        
        WriteCharCategory0(c);
    }

    private void WriteCharCategory0(char value)
    {
        bufferWriter.Write5Bits(0b00000);
        bufferWriter.WriteChar(value);
    }

    private void WriteCharCategory27(int value)
    {
        bufferWriter.Write5Bits(0b11011);
        bufferWriter.Write3Bits(value);
    }

    private void WriteCharCategory28(int value)
    {
        bufferWriter.Write5Bits(0b11100);
        bufferWriter.Write3Bits(value);
    }

    private void WriteCharCategory29(int value)
    {
        bufferWriter.Write5Bits(0b11101);
        bufferWriter.Write3Bits(value);
    }

    private void WriteCharCategory30(int value)
    {
        bufferWriter.Write5Bits(0b11110);
        bufferWriter.Write3Bits(value);
    }

    private void WriteCharCategory31(int value)
    {
        bufferWriter.Write5Bits(0b11111);
        bufferWriter.Write4Bits(value);
    }
}