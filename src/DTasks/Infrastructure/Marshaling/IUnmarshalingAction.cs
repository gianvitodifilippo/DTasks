using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IUnmarshalingAction
{
    void UnmarshalAs<TConverter>(Type tokenType, scoped ref TConverter converter)
        where TConverter : struct, ITokenConverter;

    void UnmarshalAs(Type tokenType, ITokenConverter converter)
    {
        TokenConverterWrapper wrapper = new(converter);
        UnmarshalAs(tokenType, ref wrapper);
    }

    private readonly struct TokenConverterWrapper(ITokenConverter converter) : ITokenConverter
    {
        public T Convert<TToken, T>(TToken token) => converter.Convert<TToken, T>(token);
    }
}
