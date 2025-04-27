using DTasks.Infrastructure.Marshaling;
using Xunit.Sdk;

namespace DTasks.Infrastructure.Fakes;

internal class FakeStateMachineReader(Dictionary<string, object?> values, IDAsyncSurrogator surrogator)
{
    public bool ReadField<TField>(string fieldName, ref TField value)
    {
        if (!values.Remove(fieldName, out object? untypedValue))
            return false;

        if (untypedValue is FakeSurrogatedValue surrogatedValue)
        {
            FakeRestorationAction<TField> action =
#if NET9_0_OR_GREATER
                new(surrogatedValue, ref value);
#else
                new(surrogatedValue);
#endif

            if (!surrogator.TryRestore<TField, FakeRestorationAction<TField>>(surrogatedValue.TypeId, ref action))
                throw FailException.ForFailure("Surrogator should be able to restore its own surrogate.");

#if !NET9_0_OR_GREATER
            value = action.Value!;
#endif
            return true;
        }

        value = (TField)untypedValue!;
        return true;
    }
}


#if NET9_0_OR_GREATER
internal readonly ref struct FakeRestorationAction<TField>(FakeSurrogatedValue surrogatedValue, ref TField value) : IRestorationAction
{
    private readonly ref TField _value = ref value;

    public void RestoreAs<TConverter>(Type surrogateType, scoped ref TConverter converter)
        where TConverter : struct, ISurrogateConverter
    {
        RestoreAs(surrogateType, converter);
    }

    public void RestoreAs(Type surrogateType, ISurrogateConverter converter)
    {
        _value = surrogatedValue.Convert<TField>(surrogateType, converter);
    }
}
#else
internal struct FakeRestorationAction<TField>(FakeSurrogatedValue surrogatedValue) : IRestorationAction
{
    public TField? Value { get; private set; }

    public void RestoreAs<TConverter>(Type surrogateType, scoped ref TConverter converter)
        where TConverter : struct, ISurrogateConverter
    {
        RestoreAs(surrogateType, converter);
    }

    public void RestoreAs(Type surrogateType, ISurrogateConverter converter)
    {
        Value = surrogatedValue.Convert<TField>(surrogateType, converter);
    }
}
#endif
