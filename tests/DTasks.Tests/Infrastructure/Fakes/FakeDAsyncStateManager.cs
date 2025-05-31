using System.Diagnostics;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using DTasks.Inspection.Dynamic;
using DTasks.Utils;
using Xunit.Sdk;

namespace DTasks.Infrastructure.Fakes;

internal sealed class FakeDAsyncStateManager(
    FakeStorage storage,
    IDAsyncStack stack,
    IDAsyncHeap heap,
    IDAsyncTypeResolver typeResolver,
    IDAsyncSurrogator surrogator) : IDAsyncStack, IDAsyncHeap
{
    private readonly DynamicStateMachineInspector _inspector = DynamicStateMachineInspector.Create(typeof(IFakeStateMachineSuspender<>), typeof(IFakeStateMachineResumer), typeResolver);

    public ValueTask DehydrateAsync<TStateMachine>(ISuspensionContext context, ref TStateMachine stateMachine, CancellationToken cancellationToken = default)
        where TStateMachine : notnull
    {
        _ = stack.DehydrateAsync(new SuspensionContext(context), ref stateMachine, cancellationToken);
        
        DAsyncId id = context.Id;
        DAsyncId parentId = context.ParentId;
        
        if (id.IsDefault)
            throw FailException.ForFailure("Id was defaulted.");

        if (id.IsFlow)
            throw FailException.ForFailure("Id was a flow id.");

        Debug.WriteLine($"Dehydrating runnable {id} ({stateMachine}) with parent {parentId}.");

        DehydratedRunnable<TStateMachine> runnable = new(_inspector, surrogator, parentId);
        runnable.Suspend(context, ref stateMachine);

        storage.Write(id, runnable);
        return default;
    }

    public ValueTask<DAsyncLink> HydrateAsync(IResumptionContext context, CancellationToken cancellationToken = default)
    {
        _ = stack.HydrateAsync(new ResumptionContext(context), cancellationToken);
        
        DAsyncId id = context.Id;
        DehydratedRunnable runnable = storage.Read(id);

        DAsyncLink link = runnable.Resume(context);
        Debug.WriteLine($"Hydrated runnable {id} with parent {link.ParentId}.");
        return new(link);
    }

    public ValueTask<DAsyncLink> HydrateAsync<TResult>(IResumptionContext context, TResult result,
        CancellationToken cancellationToken = default)
    {
        _ = stack.HydrateAsync(new ResumptionContext(context), result, cancellationToken);
        
        DAsyncId id = context.Id;
        DehydratedRunnable runnable = storage.Read(id);

        DAsyncLink link = runnable.Resume(context, result);
        Debug.WriteLine($"Hydrated runnable {id} with parent {link.ParentId}.");
        return new(link);
    }

    public ValueTask<DAsyncLink> HydrateAsync(IResumptionContext context, Exception exception, CancellationToken cancellationToken = default)
    {
        _ = stack.HydrateAsync(new ResumptionContext(context), exception, cancellationToken);
        
        DAsyncId id = context.Id;
        DehydratedRunnable runnable = storage.Read(id);

        DAsyncLink link = runnable.Resume(context, exception);
        Debug.WriteLine($"Hydrated runnable {id} with parent {link.ParentId}.");
        return new(link);
    }

    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        _ = stack.FlushAsync(cancellationToken);
        return default;
    }

    public Task SaveAsync<TKey, TValue>(TKey key, TValue value, CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        _ = heap.SaveAsync(key, value, cancellationToken);
        
        storage.Save(key, value);
        return Task.CompletedTask;
    }

    public Task<Option<TValue>> LoadAsync<TKey, TValue>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        _ = heap.LoadAsync<TKey, TValue>(key, cancellationToken);
        
        return Task.FromResult(storage.Load<TValue>(key));
    }

    public Task DeleteAsync<TKey>(TKey key, CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        _ = heap.DeleteAsync(key, cancellationToken);
        
        storage.Delete(key);
        return Task.CompletedTask;
    }

    private class SuspensionContext(ISuspensionContext context) : ISuspensionContext
    {
        public DAsyncId ParentId { get; } = context.ParentId;

        public DAsyncId Id { get; } = context.Id;

        public bool IsSuspended<TAwaiter>(ref TAwaiter awaiter) => throw new NotImplementedException();
    }

    private class ResumptionContext(IResumptionContext context) : IResumptionContext
    {
        public DAsyncId Id { get; } = context.Id;
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

    IDAsyncRunnable Resume(FakeStateMachineReader reader, Exception exception);
}
