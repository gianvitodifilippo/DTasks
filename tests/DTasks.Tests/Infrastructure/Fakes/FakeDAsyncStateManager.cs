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

    public ValueTask DehydrateAsync<TStateMachine>(IDehydrationContext context, ref TStateMachine stateMachine, CancellationToken cancellationToken = default)
        where TStateMachine : notnull
    {
        _ = stack.DehydrateAsync(new DehydrationContext(context), ref stateMachine, cancellationToken);
        
        DAsyncId id = context.Id;
        DAsyncId parentId = context.ParentId;
        
        if (id.IsDefault)
            throw FailException.ForFailure("Id was defaulted.");

        if (id.IsFlow)
            throw FailException.ForFailure("Id was a flow id.");

        Debug.WriteLine($"Dehydrating runnable {id} ({stateMachine}) with parent {parentId}.");

        DehydratedRunnable<TStateMachine> runnable = new(_inspector, surrogator, parentId);
        runnable.Suspend(context, ref stateMachine);

        storage.WriteRunnable(id, runnable);
        return default;
    }

    public ValueTask DehydrateCompletedAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        _ = stack.DehydrateCompletedAsync(id, cancellationToken);
        
        if (id.IsDefault)
            throw FailException.ForFailure("Id was defaulted.");

        if (id.IsFlow)
            throw FailException.ForFailure("Id was a flow id.");

        Debug.WriteLine($"Dehydrating completed runnable {id}.");

        DehydratedResult runnable = new();
        storage.WriteRunnable(id, runnable);
        return default;
    }

    public ValueTask DehydrateCompletedAsync<TResult>(DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        _ = stack.DehydrateCompletedAsync(id, result, cancellationToken);
        
        if (id.IsDefault)
            throw FailException.ForFailure("Id was defaulted.");

        if (id.IsFlow)
            throw FailException.ForFailure("Id was a flow id.");

        Debug.WriteLine($"Dehydrating completed runnable {id}.");

        DehydratedResult<TResult> runnable = new(result);
        storage.WriteRunnable(id, runnable);
        return default;
    }

    public ValueTask DehydrateCompletedAsync(DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        _ = stack.DehydrateCompletedAsync(id, exception, cancellationToken);
        
        if (id.IsDefault)
            throw FailException.ForFailure("Id was defaulted.");

        if (id.IsFlow)
            throw FailException.ForFailure("Id was a flow id.");

        Debug.WriteLine($"Dehydrating completed runnable {id}.");

        DehydratedException runnable = new(exception);
        storage.WriteRunnable(id, runnable);
        return default;
    }

    public ValueTask<DAsyncLink> HydrateAsync(IHydrationContext context, CancellationToken cancellationToken = default)
    {
        _ = stack.HydrateAsync(new HydrationContext(context), cancellationToken);
        
        DAsyncId id = context.Id;
        DehydratedRunnable runnable = storage.ReadRunnable(id);
        storage.DeleteRunnable(id);

        DAsyncLink link = runnable.Resume(context);
        Debug.WriteLine($"Hydrated runnable {id} with parent {link.ParentId}.");
        return new(link);
    }

    public ValueTask<DAsyncLink> HydrateAsync<TResult>(IHydrationContext context, TResult result,
        CancellationToken cancellationToken = default)
    {
        _ = stack.HydrateAsync(new HydrationContext(context), result, cancellationToken);
        
        DAsyncId id = context.Id;
        DehydratedRunnable runnable = storage.ReadRunnable(id);
        storage.DeleteRunnable(id);

        DAsyncLink link = runnable.Resume(context, result);
        Debug.WriteLine($"Hydrated runnable {id} with parent {link.ParentId}.");
        return new(link);
    }

    public ValueTask<DAsyncLink> HydrateAsync(IHydrationContext context, Exception exception, CancellationToken cancellationToken = default)
    {
        _ = stack.HydrateAsync(new HydrationContext(context), exception, cancellationToken);
        
        DAsyncId id = context.Id;
        DehydratedRunnable runnable = storage.ReadRunnable(id);
        storage.DeleteRunnable(id);

        DAsyncLink link = runnable.Resume(context, exception);
        Debug.WriteLine($"Hydrated runnable {id} with parent {link.ParentId}.");
        return new(link);
    }

    public ValueTask LinkAsync(ILinkContext context, CancellationToken cancellationToken = default)
    {
        _ = stack.LinkAsync(new LinkContext(context), cancellationToken);
        
        DAsyncId id = context.Id;
        DehydratedRunnable runnable = storage.ReadRunnable(id);

        bool hasLinked = runnable.Link(context);
        if (!hasLinked)
        {
            storage.DeleteRunnable(id);
        }
        
        return default;
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

    private class DehydrationContext(IDehydrationContext context) : IDehydrationContext
    {
        public DAsyncId ParentId { get; } = context.ParentId;

        public DAsyncId Id { get; } = context.Id;

        public bool IsSuspended<TAwaiter>(ref TAwaiter awaiter) => throw new NotImplementedException();
    }

    private class HydrationContext(IHydrationContext context) : IHydrationContext
    {
        public DAsyncId Id { get; } = context.Id;
    }

    private class LinkContext(ILinkContext context) : ILinkContext
    {
        public DAsyncId Id { get; } = context.Id;

        public DAsyncId ParentId { get; } = context.ParentId;

        public void SetResult() => throw new NotImplementedException();

        public void SetResult<TResult>(TResult result) => throw new NotImplementedException();

        public void SetException(Exception exception) => throw new NotImplementedException();
    }
}

internal interface IFakeStateMachineSuspender<TStateMachine>
    where TStateMachine : notnull
{
    void Suspend(ref TStateMachine stateMachine, IDehydrationContext dehydrationContext, FakeStateMachineWriter writer);
}

internal interface IFakeStateMachineResumer
{
    IDAsyncRunnable Resume(FakeStateMachineReader reader);

    IDAsyncRunnable Resume<TResult>(FakeStateMachineReader reader, TResult result);

    IDAsyncRunnable Resume(FakeStateMachineReader reader, Exception exception);
}
