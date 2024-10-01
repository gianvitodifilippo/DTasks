using DTasks.Serialization;
using DTasks.Storage;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DTasks.Hosting;

public abstract class BinaryDTaskHost<TContext, TStack, THeap> : DTaskHost<TContext>
    where TStack : IFlowStack
    where THeap : IFlowHeap
{
    protected abstract IDTaskStorage<TStack> Storage { get; }

    protected abstract IDTaskConverter<THeap> Converter { get; }

    protected abstract IDistributedLockProvider LockProvider { get; }

    protected override async Task OnWhenAllAsync(FlowId id, IDTaskScope scope, IEnumerable<DTask> tasks, CancellationToken cancellationToken)
    {
        byte branchIndex = 0;
        FlowId mainId = FlowId.NewAggregate(FlowKind.WhenAll);

        foreach (DTask task in tasks)
        {
            DTask.DAwaiter awaiter = task.GetDAwaiter();
            if (await awaiter.IsCompletedAsync())
                continue;

            FlowId branchId = mainId.GetBranchId(branchIndex, task.IsStateful);
            await SuspendBranchAsync(branchId, scope, Unsafe.As<DTask.DAwaiter, DTaskSuspender>(ref awaiter), cancellationToken);

            Debug.Assert(branchIndex < byte.MaxValue, "Aggregate task index overflow."); // TODO: support more than 255 tasks
            branchIndex++;
        }

        HashSet<byte> branchIndexes = new(branchIndex);
        for (byte i = 0; i < branchIndex; i++)
        {
            branchIndexes.Add(i);
        }

        var context = new WhenAllContext(branchIndexes, id);
        await SaveValueAsync(mainId, context, cancellationToken);
    }

    protected sealed override Task SuspendCoreAsync(TContext context, IDTaskScope scope, DTask.DAwaiter awaiter, CancellationToken cancellationToken)
    {
        return SuspendCoreAsync(context, scope, Unsafe.As<DTask.DAwaiter, DTaskSuspender>(ref awaiter), cancellationToken);
    }

    protected sealed override Task SuspendCoreAsync<TResult>(TContext context, IDTaskScope scope, DTask<TResult>.DAwaiter awaiter, CancellationToken cancellationToken)
    {
        return SuspendCoreAsync(context, scope, Unsafe.As<DTask<TResult>.DAwaiter, DTaskSuspender>(ref awaiter), cancellationToken);
    }

    private async Task SuspendCoreAsync(TContext context, IDTaskScope scope, DTaskSuspender suspender, CancellationToken cancellationToken)
    {
        BinaryStateHandler stateHandler = new(this);
        stateHandler.Heap = Converter.CreateHeap(scope);

        bool isStateful = suspender.IsStateful;
        FlowId id = FlowId.NewHosted(isStateful);

        if (isStateful)
        {
            stateHandler.Stack = Storage.CreateStack();

            ReadOnlyMemory<byte> contextBytes = Converter.Serialize(context);
            EnsureNotEmptyOnSerialize(contextBytes);
            stateHandler.Stack.Push(contextBytes);

            suspender.SaveState(ref stateHandler);

            stateHandler.Heap.StackCount = stateHandler.StackCount;
            ReadOnlyMemory<byte> heapBytes = Converter.SerializeHeap(ref stateHandler.Heap);
            EnsureNotEmptyOnSerialize(heapBytes);
            stateHandler.Stack.Push(heapBytes);

            await Storage.SaveStackAsync(id, ref stateHandler.Stack, cancellationToken);
        }
        else
        {
            await SaveValueAsync(id, context, cancellationToken);
        }

        await OnSuspendedAsync(id, scope, Unsafe.As<DTaskSuspender, DTask.DAwaiter>(ref suspender), cancellationToken);
    }

    protected sealed override Task ResumeCoreAsync(FlowId id, IDTaskScope scope, CancellationToken cancellationToken)
    {
        if (id.IsStateful)
            return ResumeStatefulAsync(id, scope, DTask.CompletedTask, cancellationToken);

        return id.Kind switch
        {
            FlowKind.Hosted => CompleteStatelessHostedAsync(id, cancellationToken),
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
            FlowKind.Hosted => CompleteStatelessHostedAsync(id, result, cancellationToken),
            FlowKind.WhenAll => CompleteWhenAllAsync(id, scope, result, cancellationToken),
            _ => throw new NotSupportedException() // TODO: Message
        };
    }

    private async Task ResumeStatefulAsync(FlowId id, IDTaskScope scope, DTask resultTask, CancellationToken cancellationToken)
    {
        BinaryStateHandler stateHandler = new(this);
        stateHandler.Stack = await Storage.LoadStackAsync(id, cancellationToken);

        ReadOnlyMemory<byte> heapBytes = await stateHandler.Stack.PopAsync(cancellationToken);
        EnsureNotEmptyOnDeserialize(id, heapBytes);
        stateHandler.Heap = Converter.DeserializeHeap(scope, heapBytes.Span);

        DTask.DAwaiter awaiter = default;
        stateHandler.StackCount = stateHandler.Heap.StackCount;
        if (stateHandler.StackCount == 0)
            throw new CorruptedDFlowException(id);

        while (stateHandler.StackCount > 0)
        {
            ReadOnlyMemory<byte> stateMachineBytes = await stateHandler.Stack.PopAsync(cancellationToken);
            EnsureNotEmptyOnDeserialize(id, stateMachineBytes);
            resultTask = Converter.DeserializeStateMachine(ref stateHandler.Heap, stateMachineBytes.Span, resultTask);

            stateHandler.StackCount--;
            awaiter = resultTask.GetDAwaiter();

            if (!await awaiter.IsCompletedAsync())
            {
                awaiter.SaveState(ref stateHandler);

                stateHandler.Heap.StackCount = stateHandler.StackCount;
                ReadOnlyMemory<byte> bytes = Converter.SerializeHeap(ref stateHandler.Heap);
                EnsureNotEmptyOnSerialize(bytes);
                stateHandler.Stack.Push(bytes);

                await Storage.SaveStackAsync(id, ref stateHandler.Stack, cancellationToken);
                await OnSuspendedAsync(id, scope, awaiter, cancellationToken);
                return;
            }
        }

        ReadOnlyMemory<byte> contextBytes;
        switch (id.Kind)
        {
            case FlowKind.Hosted:
                contextBytes = await stateHandler.Stack.PopAsync(cancellationToken);
                EnsureNotEmptyOnDeserialize(id, contextBytes);
                TContext hostContext = Converter.Deserialize<TContext>(contextBytes.Span);

                await OnCompletedAsync(id, hostContext, awaiter, cancellationToken);
                break;

            case FlowKind.WhenAll:
                await CompleteWhenAllAsync(id, scope, awaiter, cancellationToken);
                break;

            default:
                throw new NotSupportedException(); // TODO: Message
        }

        await Storage.ClearStackAsync(id, ref stateHandler.Stack, cancellationToken);
    }

    private async Task CompleteStatelessHostedAsync(FlowId id, CancellationToken cancellationToken)
    {
        TContext context = await LoadValueAsync<TContext>(id, cancellationToken);
        await OnCompletedAsync(id, context, cancellationToken);
        await Storage.ClearValueAsync(id, cancellationToken);
    }

    private async Task CompleteStatelessHostedAsync<TResult>(FlowId id, TResult result, CancellationToken cancellationToken)
    {
        TContext context = await LoadValueAsync<TContext>(id, cancellationToken);
        await OnCompletedAsync(id, context, result, cancellationToken);
        await Storage.ClearValueAsync(id, cancellationToken);
    }

    private Task CompleteWhenAllAsync(FlowId id, IDTaskScope scope, DTask.DAwaiter awaiter, CancellationToken cancellationToken)
    {
        DAwaiterChildCompletionAction action = new(awaiter);
        return CompleteWhenAllCoreAsync(id, scope, action, cancellationToken);
    }

    private Task CompleteWhenAllAsync(FlowId id, IDTaskScope scope, CancellationToken cancellationToken)
    {
        VoidChildCompletionAction action = new();
        return CompleteWhenAllCoreAsync(id, scope, action, cancellationToken);
    }

    private Task CompleteWhenAllAsync<TResult>(FlowId id, IDTaskScope scope, TResult result, CancellationToken cancellationToken)
    {
        ResultChildCompletionAction<TResult> action = new(result);
        return CompleteWhenAllCoreAsync(id, scope, action, cancellationToken);
    }

    private async Task CompleteWhenAllCoreAsync<TAction>(FlowId id, IDTaskScope scope, TAction action, CancellationToken cancellationToken)
        where TAction : ICompletionAction
    {
        FlowId mainId = id.GetMainId();
        WhenAllContext context;

        try
        {
            await using (await LockProvider.LockAsync(mainId, cancellationToken))
            {
                context = await LoadValueAsync<WhenAllContext>(mainId, cancellationToken);

                if (context.BranchIndexes is null)
                    throw new CorruptedDFlowException(mainId);

                byte branchIndex = id.BranchIndex;
                if (!context.BranchIndexes.Remove(branchIndex))
                    throw new InvalidFlowIdException(id);

                if (context.BranchIndexes.Count != 0)
                {
                    await SaveValueAsync(mainId, context, cancellationToken);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            CorruptedDFlowException.ThrowIfRethrowable(mainId, ex);
            throw;
        }

        await action.CompleteAsync(this, context.ParentFlowId, scope, cancellationToken);

        try
        {
            await Storage.ClearValueAsync(mainId, cancellationToken);
        }
        catch (Exception ex)
        {
            CorruptedDFlowException.ThrowIfRethrowable(mainId, ex);
            throw;
        }
    }

    private async Task SuspendBranchAsync(FlowId branchId, IDTaskScope scope, DTaskSuspender suspender, CancellationToken cancellationToken)
    {
        BinaryStateHandler stateHandler = new(this);

        if (branchId.IsStateful)
        {
            stateHandler.Stack = Storage.CreateStack();
            stateHandler.Heap = Converter.CreateHeap(scope);

            suspender.SaveState(ref stateHandler);

            ReadOnlyMemory<byte> heapBytes = Converter.SerializeHeap(ref stateHandler.Heap);
            EnsureNotEmptyOnSerialize(heapBytes);
            stateHandler.Stack.Push(heapBytes);

            await Storage.SaveStackAsync(branchId, ref stateHandler.Stack, cancellationToken);
        }

        await OnSuspendedAsync(branchId, scope, Unsafe.As<DTaskSuspender, DTask.DAwaiter>(ref suspender), cancellationToken);
    }

    private async Task<T> LoadValueAsync<T>(FlowId id, CancellationToken cancellationToken)
    {
        ReadOnlyMemory<byte> contextBytes = await Storage.LoadValueAsync(id, cancellationToken);
        EnsureNotEmptyOnDeserialize(id, contextBytes);
        return Converter.Deserialize<T>(contextBytes.Span);
    }

    private async Task SaveValueAsync<T>(FlowId id, T value, CancellationToken cancellationToken)
    {
        ReadOnlyMemory<byte> contextBytes = Converter.Serialize(value);
        EnsureNotEmptyOnSerialize(contextBytes);
        await Storage.SaveValueAsync(id, contextBytes, cancellationToken);
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
        public uint StackCount;
        public TStack Stack;
        public THeap Heap;

        void IStateHandler.SaveStateMachine<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo info)
        {
            ReadOnlyMemory<byte> bytes = host.Converter.SerializeStateMachine(ref Heap, ref stateMachine, info);
            EnsureNotEmptyOnSerialize(bytes);

            Stack.Push(bytes);
            StackCount++;
        }
    }

    private readonly struct DTaskSuspender(DTask task)
    {
        public bool IsStateful => task.IsStateful;

        public void SaveState(ref BinaryStateHandler handler)
        {
            task.AssertNotRunning();
            task.SaveState(ref handler);
            Debug.Assert(handler.StackCount > 0, "Expected the stack to contain at least one item in a stateful flow.");
        }

        public Task SuspendAsync<THandler>(ref THandler handler, CancellationToken cancellationToken = default)
            where THandler : ISuspensionHandler
        {
            task.AssertSuspended();
            return task.SuspendAsync(ref handler, cancellationToken);
        }
    }

    private interface ICompletionAction // enables allocation-free delegates
    {
        Task CompleteAsync(BinaryDTaskHost<TContext, TStack, THeap> host, FlowId id, IDTaskScope scope, CancellationToken cancellationToken);
    }

    private readonly struct DAwaiterChildCompletionAction(DTask.DAwaiter awaiter) : ICompletionAction
    {
        Task ICompletionAction.CompleteAsync(BinaryDTaskHost<TContext, TStack, THeap> host, FlowId id, IDTaskScope scope, CancellationToken cancellationToken)
            => host.OnChildCompletedAsync(id, scope, awaiter, cancellationToken);
    }

    private readonly struct VoidChildCompletionAction : ICompletionAction
    {
        Task ICompletionAction.CompleteAsync(BinaryDTaskHost<TContext, TStack, THeap> host, FlowId id, IDTaskScope scope, CancellationToken cancellationToken)
            => host.OnChildCompletedAsync(id, scope, cancellationToken);
    }

    private readonly struct ResultChildCompletionAction<TResult>(TResult result) : ICompletionAction
    {
        Task ICompletionAction.CompleteAsync(BinaryDTaskHost<TContext, TStack, THeap> host, FlowId id, IDTaskScope scope, CancellationToken cancellationToken)
            => host.OnChildCompletedAsync(id, scope, result, cancellationToken);
    }
}
