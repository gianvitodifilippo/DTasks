using System.Buffers;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using DTasks.Utils;

namespace DTasks.Serialization;

public sealed class BinaryDAsyncStack(IStateMachineSerializer stateMachineSerializer, IDAsyncStorage storage) : IDAsyncStack
{
    public ValueTask DehydrateAsync<TStateMachine>(ISuspensionContext context, ref TStateMachine stateMachine, CancellationToken cancellationToken = default)
        where TStateMachine : notnull
    {
        ArrayBufferWriter<byte> buffer = new();
        stateMachineSerializer.SerializeStateMachine(buffer, context, ref stateMachine);
        ReadOnlyMemory<byte> memory = buffer.WrittenMemory;
        return new(storage.SaveAsync(context.Id, memory, cancellationToken));
    }

    public async ValueTask<DAsyncLink> HydrateAsync(IResumptionContext context, CancellationToken cancellationToken = default)
    {
        Option<ReadOnlyMemory<byte>> loadResult = await storage.LoadAsync(context.Id, cancellationToken);
        if (!loadResult.HasValue)
            throw new InvalidOperationException("Invalid id."); // TODO: Improve error message

        await storage.DeleteAsync(context.Id, cancellationToken);
        return stateMachineSerializer.DeserializeStateMachine(context, loadResult.Value.Span);
    }

    public async ValueTask<DAsyncLink> HydrateAsync<TResult>(IResumptionContext context, TResult result, CancellationToken cancellationToken = default)
    {
        Option<ReadOnlyMemory<byte>> loadResult = await storage.LoadAsync(context.Id, cancellationToken);
        if (!loadResult.HasValue)
            throw new InvalidOperationException("Invalid id.");

        await storage.DeleteAsync(context.Id, cancellationToken);
        return stateMachineSerializer.DeserializeStateMachine(context, loadResult.Value.Span, result);
    }

    public async ValueTask<DAsyncLink> HydrateAsync(IResumptionContext context, Exception exception, CancellationToken cancellationToken = default)
    {
        Option<ReadOnlyMemory<byte>> loadResult = await storage.LoadAsync(context.Id, cancellationToken);
        if (!loadResult.HasValue)
            throw new InvalidOperationException("Invalid id.");

        await storage.DeleteAsync(context.Id, cancellationToken);
        return stateMachineSerializer.DeserializeStateMachine(context, loadResult.Value.Span, exception);
    }

    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        // TODO
        return default;
    }
}