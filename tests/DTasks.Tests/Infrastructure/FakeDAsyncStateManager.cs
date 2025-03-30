using DTasks.Inspection;
using DTasks.Inspection.Dynamic;
using DTasks.Marshaling;
using System.Diagnostics;
using Xunit.Sdk;

namespace DTasks.Infrastructure;

internal interface IFakeStateMachineSuspender<TStateMachine>
    where TStateMachine : notnull
{
    void Suspend(ref TStateMachine stateMachine, ISuspensionContext suspensionContext, FakeStateMachineWriter writer);
}

internal interface IFakeStateMachineResumer
{
    IDAsyncRunnable Resume(FakeStateMachineReader reader);

    IDAsyncRunnable Resume<TResult>(FakeStateMachineReader reader, TResult result);
}

internal abstract class MarshaledValue(TypeId typeId)
{
    public TypeId TypeId { get; } = typeId;

    public abstract TField Convert<TField>(Type tokenType, ITokenConverter converter);
}

internal sealed class MarshaledValue<TToken>(TypeId typeId, TToken token) : MarshaledValue(typeId)
{
    public override TField Convert<TField>(Type tokenType, ITokenConverter converter)
    {
        if (!tokenType.IsInstanceOfType(token))
            throw FailException.ForFailure($"Expected {token} to be assignable to {tokenType}.");

        return converter.Convert<TToken, TField>(token);
    }
}

internal class FakeStateMachineWriter(Dictionary<string, object?> values, IDAsyncMarshaler marshaler)
{
    public void WriteField<TField>(string fieldName, TField value)
    {
        FakeMarshalingAction action = new(fieldName, values);
        if (marshaler.TryMarshal(in value, ref action))
            return;

        values.Add(fieldName, value);
    }
}

internal class FakeStateMachineReader(Dictionary<string, object?> values, IDAsyncMarshaler marshaler)
{
    public bool ReadField<TField>(string fieldName, ref TField value)
    {
        if (!values.Remove(fieldName, out object? untypedValue))
            return false;

        if (untypedValue is MarshaledValue marshaledValue)
        {
            FakeUnmarshalingAction<TField> action =
#if NET9_0_OR_GREATER
            new(marshaledValue, ref value);
#else
            new(marshaledValue);
#endif

            if (!marshaler.TryUnmarshal<TField, FakeUnmarshalingAction<TField>>(marshaledValue.TypeId, ref action))
                throw FailException.ForFailure("Marshaler should be able to unmarshal its own token.");

#if !NET9_0_OR_GREATER
            value = action.Value!;
#endif
            return true;
        }

        value = (TField)untypedValue!;
        return true;
    }
}

internal readonly
#if NET9_0_OR_GREATER
ref
#endif
struct FakeMarshalingAction(string fieldName, Dictionary<string, object?> values) : IMarshalingAction
{
    public void MarshalAs<TToken>(TypeId typeId, TToken token)
    {
        values[fieldName] = new MarshaledValue<TToken>(typeId, token);
    }
}

#if NET9_0_OR_GREATER
internal readonly ref struct FakeUnmarshalingAction<TField>(MarshaledValue marshaledValue, ref TField value) : IUnmarshalingAction
{
    private readonly ref TField _value = ref value;

    public void UnmarshalAs<TConverter>(Type tokenType, scoped ref TConverter converter)
        where TConverter : struct, ITokenConverter
    {
        UnmarshalAs(tokenType, converter);
    }

    public void UnmarshalAs(Type tokenType, ITokenConverter converter)
    {
        _value = marshaledValue.Convert<TField>(tokenType, converter);
    }
}
#else
internal struct FakeUnmarshalingAction<TField>(MarshaledValue marshaledValue) : IUnmarshalingAction
{
    public TField? Value { get; private set; }

    public void UnmarshalAs<TConverter>(Type tokenType, scoped ref TConverter converter)
        where TConverter : struct, ITokenConverter
    {
        UnmarshalAs(tokenType, converter);
    }

    public void UnmarshalAs(Type tokenType, ITokenConverter converter)
    {
        Value = marshaledValue.Convert<TField>(tokenType, converter);
    }
}
#endif

internal sealed class FakeDAsyncStateManager(IDAsyncMarshaler marshaler, ITypeResolver typeResolver) : IDAsyncStateManager
{
    private readonly DynamicStateMachineInspector _inspector = DynamicStateMachineInspector.Create(typeof(IFakeStateMachineSuspender<>), typeof(IFakeStateMachineResumer), typeResolver);
    private readonly Dictionary<DAsyncId, DehydratedRunnable> _runnables = [];
    private Action<DAsyncId>? _onDehydrate;

    public void OnDehydrate(Action<DAsyncId> onDehydrate)
    {
        _onDehydrate = onDehydrate;
    }

    public int Count => _runnables.Count;

    public ValueTask DehydrateAsync<TStateMachine>(DAsyncId parentId, DAsyncId id, ref TStateMachine stateMachine, ISuspensionContext suspensionContext, CancellationToken cancellationToken = default)
        where TStateMachine : notnull
    {
        if (id == default)
            throw FailException.ForFailure($"'{nameof(id)}' cannot be default.");

        if (id == DAsyncId.RootId)
            throw FailException.ForFailure($"'{nameof(id)}' cannot be {nameof(DAsyncId.RootId)}.");

        Debug.WriteLine($"Dehydrating runnable {id} ({stateMachine}) with parent {parentId}.");
        _onDehydrate?.Invoke(id);
        
        DehydratedRunnable<TStateMachine> runnable = new(_inspector, marshaler, parentId);
        runnable.Suspend(ref stateMachine, suspensionContext);

        _runnables[id] = runnable;
        return default;
    }

    public ValueTask<DAsyncLink> HydrateAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        DehydratedRunnable runnable = _runnables[id];
        _runnables.Remove(id);

        DAsyncLink link = runnable.Resume();
        Debug.WriteLine($"Hydrated task {id} with parent {link.ParentId}.");
        return new(link);
    }

    public ValueTask<DAsyncLink> HydrateAsync<TResult>(DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        DehydratedRunnable runnable = _runnables[id];
        _runnables.Remove(id);

        DAsyncLink link = runnable.Resume(result);
        Debug.WriteLine($"Hydrated task {id} with parent {link.ParentId}.");
        return new(link);
    }

    public ValueTask<DAsyncLink> HydrateAsync(DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<DAsyncId> DeleteAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        if (!_runnables.Remove(id, out DehydratedRunnable? runnable))
            throw FailException.ForFailure($"Runnable '{id}' was not found.");

        return new(runnable.ParentId);
    }

    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        return default;
    }

    private abstract class DehydratedRunnable
    {
        public abstract DAsyncId ParentId { get; }

        public abstract DAsyncLink Resume();

        public abstract DAsyncLink Resume<TResult>(TResult result);
    }

    private sealed class DehydratedRunnable<TStateMachine>(IStateMachineInspector inspector, IDAsyncMarshaler marshaler, DAsyncId parentId) : DehydratedRunnable
        where TStateMachine : notnull
    {
        private readonly Dictionary<string, object?> _values = [];

        public override DAsyncId ParentId => parentId;

        public void Suspend(ref TStateMachine stateMachine, ISuspensionContext suspensionContext)
        {
            FakeStateMachineWriter writer = new(_values, marshaler);

            var converter = (IFakeStateMachineSuspender<TStateMachine>)inspector.GetSuspender(typeof(TStateMachine));
            converter.Suspend(ref stateMachine, suspensionContext, writer);
        }

        public override DAsyncLink Resume()
        {
            FakeStateMachineReader reader = new(_values, marshaler);

            var converter = (IFakeStateMachineResumer)inspector.GetResumer(typeof(TStateMachine));
            IDAsyncRunnable runnable = converter.Resume(reader);

            return new DAsyncLink(parentId, runnable);
        }

        public override DAsyncLink Resume<TResult>(TResult result)
        {
            FakeStateMachineReader reader = new(_values, marshaler);

            var converter = (IFakeStateMachineResumer)inspector.GetResumer(typeof(TStateMachine));
            IDAsyncRunnable runnable = converter.Resume(reader, result);

            return new DAsyncLink(parentId, runnable);
        }
    }
}