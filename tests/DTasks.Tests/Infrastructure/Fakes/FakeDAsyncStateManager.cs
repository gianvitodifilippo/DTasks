using System.Diagnostics;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using DTasks.Inspection.Dynamic;
using DTasks.Utils;
using Xunit.Sdk;

namespace DTasks.Infrastructure.Fakes;

internal sealed class FakeDAsyncStateManager(
    FakeStorage storage,
    IDAsyncTypeResolver typeResolver,
    IDAsyncSurrogator surrogator) : IDAsyncStack, IDAsyncHeap
{
    private readonly DynamicStateMachineInspector _inspector = DynamicStateMachineInspector.Create(typeof(IFakeStateMachineSuspender<>), typeof(IFakeStateMachineResumer), typeResolver);

    public ValueTask DehydrateAsync<TStateMachine>(ISuspensionContext context, DAsyncId parentId, DAsyncId id, ref TStateMachine stateMachine, CancellationToken cancellationToken = default)
        where TStateMachine : notnull
    {
        if (id.IsDefault)
            throw FailException.ForFailure($"Id was defaulted.");

        if (id.IsFlowId)
            throw FailException.ForFailure($"Id was a flow id.");

        Debug.WriteLine($"Dehydrating runnable {id} ({stateMachine}) with parent {parentId}.");

        DehydratedRunnable<TStateMachine> runnable = new(_inspector, surrogator, parentId);
        runnable.Suspend(context, ref stateMachine);

        storage.Write(id, runnable);
        return default;
    }

    public ValueTask<DAsyncLink> HydrateAsync(IResumptionContext context, DAsyncId id, CancellationToken cancellationToken = default)
    {
        DehydratedRunnable runnable = storage.Read(id);

        DAsyncLink link = runnable.Resume(context);
        Debug.WriteLine($"Hydrated runnable {id} with parent {link.ParentId}.");
        return new(link);
    }

    public ValueTask<DAsyncLink> HydrateAsync<TResult>(IResumptionContext context, DAsyncId id, TResult result,
        CancellationToken cancellationToken = default)
    {
        DehydratedRunnable runnable = storage.Read(id);

        DAsyncLink link = runnable.Resume(context, result);
        Debug.WriteLine($"Hydrated task {id} with parent {link.ParentId}.");
        return new(link);
    }

    public ValueTask<DAsyncLink> HydrateAsync(IResumptionContext context, DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        DehydratedRunnable runnable = storage.Read(id);

        DAsyncLink link = runnable.Resume(context, exception);
        Debug.WriteLine($"Hydrated task {id} with parent {link.ParentId}.");
        return new(link);
    }

    public ValueTask<DAsyncId> DeleteAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        DehydratedRunnable runnable = storage.Read(id);

        return new(runnable.ParentId);
    }

    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        return default;
    }

    public Task SaveAsync<TKey, TValue>(TKey key, TValue value, CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        storage.Save(key, value);
        return Task.CompletedTask;
    }

    public Task<Option<TValue>> LoadAsync<TKey, TValue>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        return Task.FromResult(storage.Load<TValue>(key));
    }

    public Task DeleteAsync<TKey>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        storage.Delete(key);
        return Task.CompletedTask;
    }
}

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
