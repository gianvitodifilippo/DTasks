using DTasks.Infrastructure.Marshaling;

namespace DTasks.Infrastructure.Fakes;

internal class FakeStateMachineWriter(Dictionary<string, object?> values, IDAsyncSurrogator surrogator)
{
    public void WriteField<TField>(string fieldName, TField value)
    {
        FakeSurrogationAction action = new(fieldName, values);
        if (surrogator.TrySurrogate(in value, ref action))
            return;

        values.Add(fieldName, value);
    }
}

internal readonly
#if NET9_0_OR_GREATER
ref
#endif
    struct FakeSurrogationAction(string fieldName, Dictionary<string, object?> values) : ISurrogationAction
{
    public void SurrogateAs<TSurrogate>(TypeId typeId, TSurrogate surrogate)
    {
        values[fieldName] = new FakeSurrogatedValue<TSurrogate>(typeId, surrogate);
    }
}
