using System.ComponentModel;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public readonly struct RestorationResult(Type surrogateType, ISurrogateConverter converter)
{
    public Type SurrogateType { get; } = surrogateType;

    public ISurrogateConverter Converter { get; } = converter;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Deconstruct(out Type surrogateType, out ISurrogateConverter converter)
    {
        surrogateType = SurrogateType;
        converter = Converter;
    }
}
