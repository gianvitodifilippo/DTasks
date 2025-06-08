using DTasks.Infrastructure.Marshaling;
using Xunit.Sdk;

namespace DTasks.Infrastructure.Fakes;

internal class FakeStateMachineReader(Dictionary<string, object?> values, IDAsyncSurrogator surrogator)
{
    public bool ReadField<TField>(string fieldName, ref TField? value)
    {
        if (!values.Remove(fieldName, out object? untypedValue))
            return false;

        if (untypedValue is FakeSurrogatedValue surrogatedValue)
        {
            FakeValueUnmarshaller unmarshaller = new(surrogatedValue.Value);
            
            if (!surrogator.TryRestore(surrogatedValue.TypeId, ref unmarshaller, out value))
                throw FailException.ForFailure("Surrogator should be able to restore its own surrogate.");
            
            return true;
        }

        if (untypedValue is FakeSurrogatedArray surrogatedArray)
        {
            FakeArrayUnmarshaller unmarshaller = new(surrogatedArray.Items);

            if (!surrogator.TryRestore(surrogatedArray.TypeId, ref unmarshaller, out value))
                throw FailException.ForFailure("Surrogator should be able to restore its own surrogate.");
            
            return true;
        }

        value = (TField)untypedValue!;
        return true;
    }
}

internal sealed class FakeValueUnmarshaller(object? value) : IUnmarshaller
{
    public TSurrogate ReadSurrogate<TSurrogate>(Type surrogateType)
    {
        if (value is not TSurrogate surrogate)
            throw FailException.ForFailure($"Value was not of type '{typeof(TSurrogate).FullName}'.");

        return surrogate;
    }

    public void BeginArray()
    {
        throw FailException.ForFailure("Value was not marshaled as an array.");
    }

    public void EndArray()
    {
        throw FailException.ForFailure("Value was not marshaled as an array.");
    }

    public T ReadItem<T>()
    {
        throw FailException.ForFailure("Value was not marshaled as an array.");
    }
}

internal sealed class FakeArrayUnmarshaller(object?[] items) : IUnmarshaller
{
    private int _readCount;
    
    public TSurrogate ReadSurrogate<TSurrogate>(Type surrogateType)
    {
        throw FailException.ForFailure("Value was not marshaled as an object.");
    }

    public void BeginArray()
    {
    }

    public void EndArray()
    {
    }

    public T ReadItem<T>()
    {
        if (items[_readCount++] is not T value)
            throw FailException.ForFailure($"Item was not of type '{typeof(T).FullName}'.");

        return value;
    }
}
