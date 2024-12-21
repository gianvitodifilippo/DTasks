namespace DTasks.Marshaling;

public static class DAsyncMarshalerExtensions
{
    public static bool TryMarshal<T>(this IDAsyncMarshaler marshaler, in T value, IMarshalingAction action)
    {
        MarshalingActionWrapper wrapper = new(action);
        return marshaler.TryMarshal(in value, ref wrapper);
    }

    public static bool TryUnmarshal<T>(this IDAsyncMarshaler marshaler, TypeId typeId, IUnmarshalingAction action)
    {
        UnmarshalingActionWrapper wrapper = new(action);
        return marshaler.TryUnmarshal<T, UnmarshalingActionWrapper>(typeId, ref wrapper);
    }

    public static bool TryUnmarshal<T>(this IDAsyncMarshaler marshaler, TypeId typeId, out UnmarshalResult result)
    {
        UnmarshalResultAction action = new();
        if (!marshaler.TryUnmarshal<T, UnmarshalResultAction>(typeId, ref action))
        {
            result = default;
            return false;
        }

        result = action.ToResult();
        return true;
    }

    private readonly struct MarshalingActionWrapper(IMarshalingAction action) : IMarshalingAction
    {
        public void MarshalAs<TToken>(TypeId typeId, TToken token) => action.MarshalAs(typeId, token);
    }

    private readonly struct UnmarshalingActionWrapper(IUnmarshalingAction action) : IUnmarshalingAction
    {
        public void UnmarshalAs<TConverter>(Type tokenType, ref TConverter converter) where TConverter : struct, ITokenConverter
            => action.UnmarshalAs(tokenType, ref converter);
 
        public void UnmarshalAs(Type tokenType, ITokenConverter converter)
            => action.UnmarshalAs(tokenType, converter);
    }

    private struct UnmarshalResultAction : IUnmarshalingAction
    {
        public Type TokenType { get; private set; }

        public ITokenConverter Converter { get; private set; }

        public readonly UnmarshalResult ToResult() => new(TokenType, Converter);

        public void UnmarshalAs<TConverter>(Type tokenType, ref TConverter converter)
            where TConverter : struct, ITokenConverter
        {
            TokenType = tokenType;
            Converter = converter;
        }

        public void UnmarshalAs(Type tokenType, ITokenConverter converter)
        {
            TokenType = tokenType;
            Converter = converter;
        }
    }
}
