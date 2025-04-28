using DTasks.Infrastructure.Marshaling;
using Xunit.Sdk;

namespace DTasks.Infrastructure.Fakes;

internal abstract class FakeSurrogatedValue(TypeId typeId)
{
    public TypeId TypeId { get; } = typeId;

    public abstract TField Convert<TField>(Type surrogateType, ISurrogateConverter converter);
}

internal sealed class FakeSurrogatedValue<TSurrogate>(TypeId typeId, TSurrogate surrogate) : FakeSurrogatedValue(typeId)
{
    public override TField Convert<TField>(Type surrogateType, ISurrogateConverter converter)
    {
        if (!surrogateType.IsInstanceOfType(surrogate))
            throw FailException.ForFailure($"Expected {surrogate} to be assignable to {surrogateType}.");

        return converter.Convert<TSurrogate, TField>(surrogate);
    }
}
