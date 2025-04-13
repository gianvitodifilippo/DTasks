using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IRestorationAction
{
    void RestoreAs<TConverter>(Type surrogateType, scoped ref TConverter converter)
        where TConverter : struct, ISurrogateConverter;

    void RestoreAs(Type surrogateType, ISurrogateConverter converter)
    {
        SurrogateConverterWrapper wrapper = new(converter);
        RestoreAs(surrogateType, ref wrapper);
    }

    private readonly struct SurrogateConverterWrapper(ISurrogateConverter converter) : ISurrogateConverter
    {
        public T Convert<TSurrogate, T>(TSurrogate surrogate) => converter.Convert<TSurrogate, T>(surrogate);
    }
}
