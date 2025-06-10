using System.Buffers;
using DTasks.Infrastructure.State;
using DTasks.Utils;

namespace DTasks.Serialization;

public sealed class BinaryDAsyncStack(IStateMachineSerializer stateMachineSerializer, IDAsyncStorage storage) : IDAsyncStack
{
    public ValueTask DehydrateAsync<TStateMachine>(IDehydrationContext context, ref TStateMachine stateMachine, CancellationToken cancellationToken = default)
        where TStateMachine : notnull
    {
        ArrayBufferWriter<byte> buffer = new();
        stateMachineSerializer.Serialize(context, buffer, ref stateMachine);
        ReadOnlyMemory<byte> memory = buffer.WrittenMemory;
        return new(storage.SaveAsync(context.Id, memory, cancellationToken));
    }

    public ValueTask DehydrateCompletedAsync(DAsyncId id, CancellationToken cancellationToken = default)
    {
        ArrayBufferWriter<byte> buffer = new();
        stateMachineSerializer.SerializeComplete(id, buffer);
        ReadOnlyMemory<byte> memory = buffer.WrittenMemory;
        return new(storage.SaveAsync(id, memory, cancellationToken));
    }

    public ValueTask DehydrateCompletedAsync<TResult>(DAsyncId id, TResult result, CancellationToken cancellationToken = default)
    {
        ArrayBufferWriter<byte> buffer = new();
        stateMachineSerializer.SerializeComplete(id, buffer, result);
        ReadOnlyMemory<byte> memory = buffer.WrittenMemory;
        return new(storage.SaveAsync(id, memory, cancellationToken));
    }

    public ValueTask DehydrateCompletedAsync(DAsyncId id, Exception exception, CancellationToken cancellationToken = default)
    {
        ArrayBufferWriter<byte> buffer = new();
        stateMachineSerializer.SerializeComplete(id, buffer, exception);
        ReadOnlyMemory<byte> memory = buffer.WrittenMemory;
        return new(storage.SaveAsync(id, memory, cancellationToken));
    }

    public async ValueTask<DAsyncLink> HydrateAsync(IHydrationContext context, CancellationToken cancellationToken = default)
    {
        Option<ReadOnlyMemory<byte>> loadResult = await storage.LoadAsync(context.Id, cancellationToken);
        if (!loadResult.HasValue)
            throw new InvalidOperationException("Invalid id."); // TODO: Improve error message

        await storage.DeleteAsync(context.Id, cancellationToken);
        return stateMachineSerializer.Deserialize(context, loadResult.Value.Span);
    }

    public async ValueTask<DAsyncLink> HydrateAsync<TResult>(IHydrationContext context, TResult result, CancellationToken cancellationToken = default)
    {
        Option<ReadOnlyMemory<byte>> loadResult = await storage.LoadAsync(context.Id, cancellationToken);
        if (!loadResult.HasValue)
            throw new InvalidOperationException("Invalid id.");

        await storage.DeleteAsync(context.Id, cancellationToken);
        return stateMachineSerializer.Deserialize(context, loadResult.Value.Span, result);
    }

    public async ValueTask<DAsyncLink> HydrateAsync(IHydrationContext context, Exception exception, CancellationToken cancellationToken = default)
    {
        Option<ReadOnlyMemory<byte>> loadResult = await storage.LoadAsync(context.Id, cancellationToken);
        if (!loadResult.HasValue)
            throw new InvalidOperationException("Invalid id.");

        await storage.DeleteAsync(context.Id, cancellationToken);
        return stateMachineSerializer.Deserialize(context, loadResult.Value.Span, exception);
    }

    public async ValueTask LinkAsync(ILinkContext context, CancellationToken cancellationToken = default)
    {
        Option<ReadOnlyMemory<byte>> loadResult = await storage.LoadAsync(context.Id, cancellationToken);
        if (!loadResult.HasValue)
            throw new InvalidOperationException("Invalid id.");

        byte[] bytes = new byte[loadResult.Value.Length]; // TODO: Pool and avoid copying if it has result
        loadResult.Value.Span.CopyTo(bytes);
        bool hasLinked = stateMachineSerializer.Link(context, bytes);

        if (!hasLinked)
        {
            await storage.DeleteAsync(context.Id, cancellationToken);
        }
    }

    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        // TODO
        return default;
    }
}