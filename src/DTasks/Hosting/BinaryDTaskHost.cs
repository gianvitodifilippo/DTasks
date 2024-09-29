using DTasks.Serialization;
using DTasks.Storage;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DTasks.Hosting;

public abstract class BinaryDTaskHost<TContext, TStack, THeap> : DTaskHost<TContext>
    where TStack : IFlowStack
    where THeap : IDTaskHeap
{
    protected abstract IDTaskStorage<TStack> Storage { get; }

    protected abstract IDTaskConverter<THeap> Converter { get; }

    protected abstract IDistributedLockProvider LockProvider { get; }

    protected sealed override async Task OnWhenAllAsync(FlowId id, IDTaskScope scope, IEnumerable<DTask> tasks, CancellationToken cancellationToken)
    {
        byte remainingToComplete = 0;
        FlowId mainId = FlowId.NewAggregate(FlowKind.WhenAll);

        foreach (DTask task in tasks)
        {
            DTask.DAwaiter awaiter = task.GetDAwaiter();
            if (await awaiter.IsCompletedAsync())
                continue;

            FlowId branchId = mainId.GetBranchId(remainingToComplete);
            await SuspendBranchAsync(branchId, scope, Unsafe.As<DTask.DAwaiter, DTaskSuspender>(ref awaiter), cancellationToken);

            Debug.Assert(remainingToComplete < byte.MaxValue, "Aggregate task index overflow."); // TODO: support more than 255 tasks
            remainingToComplete++;
        }

        var context = new WhenAllContext(remainingToComplete, id);
        ReadOnlyMemory<byte> contextBytes = Converter.Serialize(context);
        EnsureNotEmptyOnSerialize(contextBytes);

        await Storage.SaveValueAsync(mainId, contextBytes, cancellationToken);
    }

    protected sealed override async Task SuspendCoreAsync(TContext context, IDTaskScope scope, DTaskSuspender suspender, CancellationToken cancellationToken)
    {
        BinaryStateHandler stateHandler = new(this);
        stateHandler.Heap = Converter.CreateHeap(scope);

        ReadOnlyMemory<byte> contextBytes = Converter.Serialize(context);
        EnsureNotEmptyOnSerialize(contextBytes);

        bool isStateful = suspender.IsStateful;
        FlowId id = FlowId.NewHosted(isStateful);

        if (isStateful)
        {
            stateHandler.Stack = Storage.CreateStack();
            stateHandler.Stack.Push(contextBytes);

            suspender.SaveState(ref stateHandler);
            Debug.Assert(stateHandler.StackCount > 0, "Expected the stack to contain at least one item in a stateful flow.");

            stateHandler.Heap.StackCount = stateHandler.StackCount;
            ReadOnlyMemory<byte> heapBytes = Converter.SerializeHeap(ref stateHandler.Heap);
            EnsureNotEmptyOnSerialize(heapBytes);
            stateHandler.Stack.Push(heapBytes);

            await Storage.SaveStackAsync(id, ref stateHandler.Stack, cancellationToken);
        }
        else
        {
            await Storage.SaveValueAsync(id, contextBytes, cancellationToken);
        }

        await OnSuspendedAsync(id, scope, suspender, cancellationToken);
    }

    protected sealed override Task ResumeCoreAsync(FlowId id, IDTaskScope scope, CancellationToken cancellationToken)
    {
        if (id.IsStateful)
            return ResumeStatefulAsync(id, scope, DTask.CompletedTask, cancellationToken);

        return id.Kind switch
        {
            FlowKind.Hosted => CompleteHostedAsync(id, cancellationToken),
            FlowKind.WhenAll => CompleteWhenAllAsync(id, scope, cancellationToken),
            _ => throw new NotSupportedException() // TODO: Message
        };
    }

    protected sealed override Task ResumeCoreAsync<TResult>(FlowId id, IDTaskScope scope, TResult result, CancellationToken cancellationToken)
    {
        if (id.IsStateful)
            return ResumeStatefulAsync(id, scope, DTask.FromResult(result), cancellationToken);

        return id.Kind switch
        {
            FlowKind.Hosted => CompleteHostedAsync(id, result, cancellationToken),
            FlowKind.WhenAll => CompleteWhenAllAsync(id, scope, result, cancellationToken),
            _ => throw new NotSupportedException() // TODO: Message
        };
    }

    private async Task ResumeStatefulAsync(FlowId id, IDTaskScope scope, DTask resultTask, CancellationToken cancellationToken)
    {
        BinaryStateHandler stateHandler = new(this);
        stateHandler.Stack = await stateHandler.Storage.LoadStackAsync(id, cancellationToken);

        try
        {
            ReadOnlyMemory<byte> heapBytes = await stateHandler.Stack.PopAsync(cancellationToken);
            EnsureNotEmptyOnDeserialize(id, heapBytes);
            stateHandler.Heap = stateHandler.Converter.DeserializeHeap(scope, heapBytes.Span);
        }
        catch (Exception ex)
        {
            CorruptedDFlowException.ThrowIfRethrowable(id, ex);
            throw;
        }

        DTask.DAwaiter awaiter = default;
        stateHandler.StackCount = stateHandler.Heap.StackCount;
        if (stateHandler.StackCount == 0)
            throw new CorruptedDFlowException(id);

        while (stateHandler.StackCount > 0)
        {
            try
            {
                ReadOnlyMemory<byte> stateMachineBytes = await stateHandler.Stack.PopAsync(cancellationToken);
                EnsureNotEmptyOnDeserialize(id, stateMachineBytes);
                resultTask = stateHandler.Converter.DeserializeStateMachine(ref stateHandler.Heap, stateMachineBytes.Span, resultTask);
            }
            catch (Exception ex)
            {
                CorruptedDFlowException.ThrowIfRethrowable(id, ex);
                throw;
            }

            stateHandler.StackCount--;
            awaiter = resultTask.GetDAwaiter();

            if (!await awaiter.IsCompletedAsync())
            {
                awaiter.SaveState(ref stateHandler);

                stateHandler.Heap.StackCount = stateHandler.StackCount;
                ReadOnlyMemory<byte> bytes = stateHandler.Converter.SerializeHeap(ref stateHandler.Heap);
                EnsureNotEmptyOnSerialize(bytes);
                stateHandler.Stack.Push(bytes);

                await stateHandler.Storage.SaveStackAsync(id, ref stateHandler.Stack, cancellationToken);
                await stateHandler.Host.OnSuspendedAsync(id, scope, awaiter, cancellationToken);
                return;
            }
        }

        ReadOnlyMemory<byte> contextBytes;
        switch (id.Kind)
        {
            case FlowKind.Hosted:
                TContext hostContext;
                try
                {
                    contextBytes = await stateHandler.Stack.PopAsync(cancellationToken);
                    EnsureNotEmptyOnDeserialize(id, contextBytes);
                    hostContext = stateHandler.Converter.Deserialize<TContext>(contextBytes.Span);
                }
                catch (Exception ex)
                {
                    CorruptedDFlowException.ThrowIfRethrowable(id, ex);
                    throw;
                }

                await stateHandler.Host.OnHostedCompletedAsync(id, hostContext, awaiter, cancellationToken);
                break;

            case FlowKind.WhenAll:
                FlowId mainId = id.GetMainId();
                WhenAllContext whenAllContext;

                await using (await stateHandler.LockProvider.LockAsync(mainId, cancellationToken))
                {
                    try
                    {
                        contextBytes = await stateHandler.Storage.LoadValueAsync(mainId, cancellationToken);
                        EnsureNotEmptyOnDeserialize(mainId, contextBytes);
                        whenAllContext = stateHandler.Converter.Deserialize<WhenAllContext>(contextBytes.Span);
                    }
                    catch (Exception ex)
                    {
                        CorruptedDFlowException.ThrowIfRethrowable(mainId, ex);
                        throw;
                    }

                    if (--whenAllContext.RemainingToComplete != 0)
                    {
                        contextBytes = stateHandler.Converter.Serialize(whenAllContext);
                        EnsureNotEmptyOnSerialize(contextBytes);

                        await stateHandler.Storage.SaveValueAsync(mainId, contextBytes, cancellationToken);
                        return;
                    }
                }

                await OnAggregateCompletedAsync(whenAllContext.ParentFlowId, scope, awaiter, cancellationToken);
                await stateHandler.Storage.ClearValueAsync(mainId, cancellationToken);
                break;

            default:
                throw new NotSupportedException(); // TODO: Message
        }

        await stateHandler.Storage.ClearStackAsync(id, ref stateHandler.Stack, cancellationToken);
    }

    private async Task CompleteHostedAsync(FlowId id, CancellationToken cancellationToken)
    {
        TContext context;
        try
        {
            ReadOnlyMemory<byte> contextBytes = await Storage.LoadValueAsync(id, cancellationToken);
            EnsureNotEmptyOnDeserialize(id, contextBytes);
            context = Converter.Deserialize<TContext>(contextBytes.Span);
        }
        catch (Exception ex)
        {
            CorruptedDFlowException.ThrowIfRethrowable(id, ex);
            throw;
        }

        await Storage.ClearValueAsync(id, cancellationToken);
        await OnCompletedAsync(id, context, cancellationToken);
    }

    private async Task CompleteHostedAsync<TResult>(FlowId id, TResult result, CancellationToken cancellationToken)
    {
        TContext context;
        try
        {
            ReadOnlyMemory<byte> contextBytes = await Storage.LoadValueAsync(id, cancellationToken);
            EnsureNotEmptyOnDeserialize(id, contextBytes);
            context = Converter.Deserialize<TContext>(contextBytes.Span);
        }
        catch (Exception ex)
        {
            CorruptedDFlowException.ThrowIfRethrowable(id, ex);
            throw;
        }

        await Storage.ClearValueAsync(id, cancellationToken);
        await OnCompletedAsync(id, context, result, cancellationToken);
    }

    private async Task CompleteWhenAllAsync(FlowId id, IDTaskScope scope, CancellationToken cancellationToken)
    {
        FlowId mainId = id.GetMainId();
        WhenAllContext context;

        await using (await LockProvider.LockAsync(mainId, cancellationToken))
        {
            ReadOnlyMemory<byte> contextBytes;
            try
            {
                contextBytes = await Storage.LoadValueAsync(mainId, cancellationToken);
                EnsureNotEmptyOnDeserialize(mainId, contextBytes);
                context = Converter.Deserialize<WhenAllContext>(contextBytes.Span);
            }
            catch (Exception ex)
            {
                CorruptedDFlowException.ThrowIfRethrowable(mainId, ex);
                throw;
            }

            if (--context.RemainingToComplete != 0)
            {
                contextBytes = Converter.Serialize(context);
                EnsureNotEmptyOnSerialize(contextBytes);

                await Storage.SaveValueAsync(mainId, contextBytes, cancellationToken);
                return;
            }
        }

        await ResumeAsync(context.ParentFlowId, scope, cancellationToken);
        await Storage.ClearValueAsync(mainId, cancellationToken);
    }

    private async Task CompleteWhenAllAsync<TResult>(FlowId id, IDTaskScope scope, TResult result, CancellationToken cancellationToken)
    {
        FlowId mainId = id.GetMainId();
        WhenAllContext context;

        await using (await LockProvider.LockAsync(mainId, cancellationToken))
        {
            ReadOnlyMemory<byte> contextBytes;
            try
            {
                contextBytes = await Storage.LoadValueAsync(mainId, cancellationToken);
                EnsureNotEmptyOnDeserialize(mainId, contextBytes);
                context = Converter.Deserialize<WhenAllContext>(contextBytes.Span);
            }
            catch (Exception ex)
            {
                CorruptedDFlowException.ThrowIfRethrowable(mainId, ex);
                throw;
            }

            if (--context.RemainingToComplete != 0)
            {
                contextBytes = Converter.Serialize(context);
                EnsureNotEmptyOnSerialize(contextBytes);

                await Storage.SaveValueAsync(mainId, contextBytes, cancellationToken);
                return;
            }
        }

        await ResumeAsync(context.ParentFlowId, scope, result, cancellationToken);
        await Storage.ClearValueAsync(mainId, cancellationToken);
    }

    private async Task SuspendBranchAsync(FlowId branchId, IDTaskScope scope, DTaskSuspender suspender, CancellationToken cancellationToken)
    {
        BinaryStateHandler stateHandler = new(this);
        stateHandler.Stack = Storage.CreateStack();
        stateHandler.Heap = Converter.CreateHeap(scope);

        suspender.SaveState(ref stateHandler);

        bool isStateful = stateHandler.Heap.StackCount != 0;
        if (isStateful)
        {
            ReadOnlyMemory<byte> heapBytes = stateHandler.Converter.SerializeHeap(ref stateHandler.Heap);
            EnsureNotEmptyOnSerialize(heapBytes);
            stateHandler.Stack.Push(heapBytes);
        }

        await Storage.SaveStackAsync(branchId, ref stateHandler.Stack, cancellationToken);
        await OnSuspendedAsync(branchId, scope, suspender, cancellationToken);
    }

    private static void EnsureNotEmptyOnSerialize(ReadOnlyMemory<byte> bytes)
    {
        if (bytes.IsEmpty)
            throw new InvalidOperationException("Serialization produced an empty sequence of bytes.");
    }

    private static void EnsureNotEmptyOnDeserialize(FlowId id, ReadOnlyMemory<byte> bytes)
    {
        if (bytes.IsEmpty)
            throw new CorruptedDFlowException(id);
    }

    private struct BinaryStateHandler(BinaryDTaskHost<TContext, TStack, THeap> host) : IStateHandler
    {
        public BinaryDTaskHost<TContext, TStack, THeap> Host = host;
        public uint StackCount;
        public TStack Stack;
        public THeap Heap;

        public readonly IDTaskStorage<TStack> Storage => Host.Storage;

        public readonly IDTaskConverter<THeap> Converter => Host.Converter;

        public readonly IDistributedLockProvider LockProvider => Host.LockProvider;

        void IStateHandler.SaveStateMachine<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo info)
        {
            ReadOnlyMemory<byte> bytes = Host.Converter.SerializeStateMachine(ref Heap, ref stateMachine, info);
            EnsureNotEmptyOnSerialize(bytes);

            Stack.Push(bytes);
            StackCount++;
        }
    }
}
