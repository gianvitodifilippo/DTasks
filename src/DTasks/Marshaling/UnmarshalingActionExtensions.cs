namespace DTasks.Marshaling;

public static class UnmarshalingActionExtensions
{
    public static void UnmarshalAs<TToken, T>(this IUnmarshalingAction action, Type tokenType, Func<TToken, T> converter)
    {
        FuncTokenConverterWrapper<TToken, T> wrapper = new(converter);
        action.UnmarshalAs(tokenType, ref wrapper);
    }

    public static void UnmarshalAs<TAction, TToken, T>(this scoped ref TAction action, Type tokenType, Func<TToken, T> converter)
        where TAction : struct, IUnmarshalingAction
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        FuncTokenConverterWrapper<TToken, T> wrapper = new(converter);
        action.UnmarshalAs(tokenType, ref wrapper);
    }

    private readonly struct FuncTokenConverterWrapper<TToken, T>(Func<TToken, T> converter) : ITokenConverter
    {
        public TActual Convert<TActualToken, TActual>(TActualToken actualToken)
        {
            if (actualToken is not TToken token)
                throw new ArgumentException($"Expected a token of type '{typeof(TToken).Name}'.", nameof(actualToken));

            T value = converter(token);
            if (value is not TActual actualValue)
                throw new InvalidOperationException("Attempted to unmarshal a token to a value of the wrong type.");

            return actualValue;
        }
    }
}
