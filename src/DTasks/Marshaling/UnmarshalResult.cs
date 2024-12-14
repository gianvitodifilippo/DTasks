using System.ComponentModel;

namespace DTasks.Marshaling;

public readonly struct UnmarshalResult(Type tokenType, ITokenConverter converter)
{
    public Type TokenType { get; } = tokenType;

    public ITokenConverter Converter { get; } = converter;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Deconstruct(out Type tokenType, out ITokenConverter converter)
    {
        tokenType = TokenType;
        converter = Converter;
    }
}
