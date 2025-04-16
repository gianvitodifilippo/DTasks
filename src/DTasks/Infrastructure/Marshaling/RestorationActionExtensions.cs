using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class RestorationActionExtensions
{
    public static void RestoreAs<TSurrogate, T>(this IRestorationAction action, Type surrogateType, Func<TSurrogate, T> converter)
    {
        FuncSurrogateConverterWrapper<TSurrogate, T> wrapper = new(converter);
        action.RestoreAs(surrogateType, ref wrapper);
    }

    public static void RestoreAs<TAction, TSurrogate, T>(this ref TAction action, Type surrogateType, Func<TSurrogate, T> converter)
        where TAction : struct, IRestorationAction
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        FuncSurrogateConverterWrapper<TSurrogate, T> wrapper = new(converter);
        action.RestoreAs(surrogateType, ref wrapper);
    }

    private readonly struct FuncSurrogateConverterWrapper<TSurrogate, T>(Func<TSurrogate, T> converter) : ISurrogateConverter
    {
        public TActual Convert<TActualSurrogate, TActual>(TActualSurrogate actualSurrogate)
        {
            if (actualSurrogate is not TSurrogate surrogate)
                throw new ArgumentException($"Expected a surrogate of type '{typeof(TSurrogate).Name}'.", nameof(actualSurrogate));

            T value = converter(surrogate);
            if (value is not TActual actualValue)
                throw new InvalidOperationException("Attempted to restore a surrogate to a value of the wrong type.");

            return actualValue;
        }
    }
}
