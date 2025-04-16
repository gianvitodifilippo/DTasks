using DTasks.Inspection;
using DTasks.Inspection.Dynamic;
using System.Diagnostics;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using DTasks.Utils;
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

    // IDAsyncRunnable Resume(FakeStateMachineReader reader, Exception exception);
}

internal abstract class SurrogatedValue(TypeId typeId)
{
    public TypeId TypeId { get; } = typeId;

    public abstract TField Convert<TField>(Type surrogateType, ISurrogateConverter converter);
}

internal sealed class SurrogatedValue<TSurrogate>(TypeId typeId, TSurrogate surrogate) : SurrogatedValue(typeId)
{
    public override TField Convert<TField>(Type surrogateType, ISurrogateConverter converter)
    {
        if (!surrogateType.IsInstanceOfType(surrogate))
            throw FailException.ForFailure($"Expected {surrogate} to be assignable to {surrogateType}.");

        return converter.Convert<TSurrogate, TField>(surrogate);
    }
}

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

internal class FakeStateMachineReader(Dictionary<string, object?> values, IDAsyncSurrogator surrogator)
{
    public bool ReadField<TField>(string fieldName, ref TField value)
    {
        if (!values.Remove(fieldName, out object? untypedValue))
            return false;

        if (untypedValue is SurrogatedValue surrogatedValue)
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

internal readonly
#if NET9_0_OR_GREATER
ref
#endif
struct FakeSurrogationAction(string fieldName, Dictionary<string, object?> values) : ISurrogationAction
{
    public void SurrogateAs<TSurrogate>(TypeId typeId, TSurrogate surrogate)
    {
        values[fieldName] = new SurrogatedValue<TSurrogate>(typeId, surrogate);
    }
}

#if NET9_0_OR_GREATER
internal readonly ref struct FakeRestorationAction<TField>(SurrogatedValue surrogatedValue, ref TField value) : IRestorationAction
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
internal struct FakeRestorationAction<TField>(SurrogatedValue surrogatedValue) : IRestorationAction
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

internal sealed class FakeDAsyncStateManager(IDAsyncTypeResolver typeResolver) : IDAsyncStateManager, IDAsyncStack, IDAsyncHeap
{
    private readonly DynamicStateMachineInspector _inspector = DynamicStateMachineInspector.Create(typeof(IFakeStateMachineSuspender<>), typeof(IFakeStateMachineResumer), typeResolver);
    private readonly Dictionary<DAsyncId, DehydratedRunnable> _runnables = [];
    private readonly Dictionary<object, object?> _heap = [];
    private Action<DAsyncId>? _onDehydrate;

    public void OnDehydrate(Action<DAsyncId> onDehydrate)
    {
        _onDehydrate = onDehydrate;
    }

    public int Count => _runnables.Count;

    IDAsyncStack IDAsyncStateManager.Stack => this;

    IDAsyncHeap IDAsyncStateManager.Heap => this;

    public ValueTask DehydrateAsync<TStateMachine>(ISuspensionContext context, DAsyncId parentId, DAsyncId id, ref TStateMachine stateMachine,
        CancellationToken cancellationToken = default) where TStateMachine : notnull
    {
        if (id.IsDefault)
            throw FailException.ForFailure($"Id was defaulted.");

        if (id.IsFlowId)
            throw FailException.ForFailure($"Id was a flow id.");

        Debug.WriteLine($"Dehydrating runnable {id} ({stateMachine}) with parent {parentId}.");
        _onDehydrate?.Invoke(id);

        DehydratedRunnable<TStateMachine> runnable = new(_inspector, parentId);
        runnable.Suspend(context, ref stateMachine);

        _runnables.Add(id, runnable);
        return default;
    }

    public ValueTask<DAsyncLink> HydrateAsync(IResumptionContext context, DAsyncId id, CancellationToken cancellationToken = default)
    {
        DehydratedRunnable runnable = _runnables[id];
        _runnables.Remove(id);

        DAsyncLink link = runnable.Resume(context);
        Debug.WriteLine($"Hydrated task {id} with parent {link.ParentId}.");
        return new(link);
    }

    public ValueTask<DAsyncLink> HydrateAsync<TResult>(IResumptionContext context, DAsyncId id, TResult result,
        CancellationToken cancellationToken = default)
    {
        DehydratedRunnable runnable = _runnables[id];
        _runnables.Remove(id);

        DAsyncLink link = runnable.Resume(context, result);
        Debug.WriteLine($"Hydrated task {id} with parent {link.ParentId}.");
        return new(link);
    }

    public ValueTask<DAsyncLink> HydrateAsync(IResumptionContext context, DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        DehydratedRunnable runnable = _runnables[id];
        _runnables.Remove(id);

        DAsyncLink link = runnable.Resume(context, exception);
        Debug.WriteLine($"Hydrated task {id} with parent {link.ParentId}.");
        return new(link);
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

    public Task SaveAsync<TKey, TValue>(TKey key, TValue value, CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        _heap[key] = value;
        return Task.CompletedTask;
    }

    public Task<Option<TValue>> LoadAsync<TKey, TValue>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        if (!_heap.TryGetValue(key, out object? value))
            return Task.FromResult(Option<TValue>.None);

        return Task.FromResult(Option<TValue>.Some((TValue)value!));
    }

    public Task DeleteAsync<TKey>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        _heap.Remove(key);
        return Task.CompletedTask;
    }

    private abstract class DehydratedRunnable
    {
        public abstract DAsyncId ParentId { get; }

        public abstract DAsyncLink Resume(IResumptionContext context);

        public abstract DAsyncLink Resume<TResult>(IResumptionContext context, TResult result);

        public abstract DAsyncLink Resume(IResumptionContext context, Exception exception);
    }

    private sealed class DehydratedRunnable<TStateMachine>(IStateMachineInspector inspector, DAsyncId parentId) : DehydratedRunnable
        where TStateMachine : notnull
    {
        private readonly Dictionary<string, object?> _values = [];

        public override DAsyncId ParentId => parentId;

        public void Suspend(ISuspensionContext context, ref TStateMachine stateMachine)
        {
            FakeStateMachineWriter writer = new(_values, context.Surrogator);

            var converter = (IFakeStateMachineSuspender<TStateMachine>)inspector.GetSuspender(typeof(TStateMachine));
            converter.Suspend(ref stateMachine, context, writer);
        }

        public override DAsyncLink Resume(IResumptionContext context)
        {
            FakeStateMachineReader reader = new(_values, context.Surrogator);

            var converter = (IFakeStateMachineResumer)inspector.GetResumer(typeof(TStateMachine));
            IDAsyncRunnable runnable = converter.Resume(reader);

            return new DAsyncLink(parentId, runnable);
        }

        public override DAsyncLink Resume<TResult>(IResumptionContext context, TResult result)
        {
            FakeStateMachineReader reader = new(_values, context.Surrogator);

            var converter = (IFakeStateMachineResumer)inspector.GetResumer(typeof(TStateMachine));
            IDAsyncRunnable runnable = converter.Resume(reader, result);

            return new DAsyncLink(parentId, runnable);
        }

        public override DAsyncLink Resume(IResumptionContext context, Exception exception)
        {
            throw new NotImplementedException();
        }
    }
}