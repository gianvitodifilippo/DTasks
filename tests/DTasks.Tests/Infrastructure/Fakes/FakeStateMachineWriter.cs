using System.Diagnostics.CodeAnalysis;
using DTasks.Infrastructure.Marshaling;
using Xunit.Sdk;

namespace DTasks.Infrastructure.Fakes;

internal class FakeStateMachineWriter(Dictionary<string, object?> values, IDAsyncSurrogator surrogator)
{
    public void WriteField<TField>(string fieldName, TField value)
    {
        FakeMarshaller marshaller = new(fieldName, values);
        if (surrogator.TrySurrogate(in value, ref marshaller))
            return;

        if (value is DTask)
            throw FailException.ForFailure("DTasks should be surrogated.");
        
        values.Add(fieldName, value);
    }
}

internal sealed class FakeMarshaller(string fieldName, Dictionary<string, object?> values) : IMarshaller
{
    private FakeSurrogatedArray? _array;
    private int _itemCount;

    public void WriteSurrogate<TSurrogate>(TypeId typeId, in TSurrogate value)
    {
        EnsureNotWritten();
        EnsureNotBeganArray();
        
        values[fieldName] = new FakeSurrogatedValue(typeId, value);
    }

    public void BeginArray(TypeId typeId, int memberCount)
    {
        EnsureNotWritten();
        EnsureNotBeganArray();
        
        _array = new FakeSurrogatedArray(typeId, new object?[memberCount]);
    }

    public void EndArray()
    {
        EnsureNotWritten();
        EnsureBeganArray();

        values[fieldName] = _array;
        _array = null;
    }

    public void WriteItem<T>(in T value)
    {
        EnsureNotWritten();
        EnsureBeganArray();

        _array.Items[_itemCount++] = value;
    }

    private void EnsureNotWritten()
    {
        if (values.ContainsKey(fieldName))
            throw FailException.ForFailure("Surrogate was already written.");
    }

    [MemberNotNull(nameof(_array))]
    private void EnsureBeganArray()
    {
        if (_array is null)
            throw FailException.ForFailure("BeginArray was not called.");
    }

    private void EnsureNotBeganArray()
    {
        if (_array is not null)
            throw FailException.ForFailure("BeginArray was called.");
    }
}
