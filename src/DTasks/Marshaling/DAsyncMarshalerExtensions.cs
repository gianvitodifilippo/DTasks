namespace DTasks.Marshaling;

public static class DAsyncMarshalerExtensions
{
    public static bool TryMarshal<T>(this IDAsyncMarshaler marshaler, string fieldName, in T value, IMarshalingAction action)
    {
        MarshalingActionWrapper wrapper = new(action);
        return marshaler.TryMarshal(fieldName, value, ref wrapper);
    }

    public static bool TryUnmarshal<T>(this IDAsyncMarshaler marshaler, string fieldName, TypeId typeId, IUnmarshalingAction action)
    {
        UnmarshalingActionWrapper wrapper = new(action);
        return marshaler.TryUnmarshal<T, UnmarshalingActionWrapper>(fieldName, typeId, ref wrapper);
    }

    private readonly
#if NET9_0_OR_GREATER
        ref
#endif
        struct MarshalingActionWrapper(IMarshalingAction action) : IMarshalingAction
    {
        public void MarshalAs<TToken>(TypeId typeId, TToken token) => action.MarshalAs(typeId, token);
    }

    private readonly
#if NET9_0_OR_GREATER
        ref
#endif
        struct UnmarshalingActionWrapper(IUnmarshalingAction action) : IUnmarshalingAction
    {
        public void UnmarshalAs<TConverter>(Type tokenType, ref TConverter converter) where TConverter : struct, ITokenConverter
            => action.UnmarshalAs(tokenType, ref converter);
 
        public void UnmarshalAs(Type tokenType, ITokenConverter converter)
            => action.UnmarshalAs(tokenType, converter);
    }
}
