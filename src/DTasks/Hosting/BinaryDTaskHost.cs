using DTasks.Serialization;
using DTasks.Storage;

namespace DTasks.Hosting;

public abstract class BinaryDTaskHost<TFlowId, TContext, TStack, THeap> : DTaskHost<TFlowId, TContext>
    where TFlowId : notnull
    where TStack : IFlowStack
{
    protected abstract IDTaskStorage<TStack> Storage { get; }

    protected abstract IDTaskConverter<THeap> Converter { get; }

    protected abstract Task OnSuspendedAsync(TFlowId flowId, ISuspensionCallback callback, CancellationToken cancellationToken);

    protected abstract Task OnDelayAsync(TFlowId flowId, TimeSpan delay, CancellationToken cancellationToken);

    protected abstract Task OnYieldAsync(TFlowId flowId, CancellationToken cancellationToken);

    protected abstract Task OnCompletedAsync(TFlowId flowId, TContext context, CancellationToken cancellationToken);

    protected abstract Task OnCompletedAsync<TResult>(TFlowId flowId, TContext context, TResult result, CancellationToken cancellationToken);

    protected sealed override Task SuspendCoreAsync(TFlowId flowId, TContext context, IDTaskScope scope, DTask.DAwaiter awaiter, CancellationToken cancellationToken)
    {
        FlowHandler handler = new(flowId, this);
        return handler.SuspendAsync(scope, context, awaiter, cancellationToken);
    }

    protected sealed override Task ResumeCoreAsync(TFlowId flowId, IDTaskScope scope, DTask resultTask, CancellationToken cancellationToken)
    {
        FlowHandler handler = new(flowId, this);
        return handler.ResumeAsync(scope, resultTask, cancellationToken);
    }

    private struct FlowHandler(TFlowId flowId, BinaryDTaskHost<TFlowId, TContext, TStack, THeap> host) : IStateHandler, ISuspensionHandler, ICompletionHandler
    {
        private TStack _stack;
        private THeap _heap;
        private TContext _context;

        public async Task SuspendAsync(IDTaskScope scope, TContext context, DTask.DAwaiter awaiter, CancellationToken cancellationToken)
        {
            _stack = host.Storage.CreateStack();
            _heap = host.Converter.CreateHeap(scope);

            awaiter.SaveState(ref this);

            ReadOnlyMemory<byte> heapBytes = host.Converter.SerializeHeap(ref _heap);
            EnsureNotEmptyOnSerialize(heapBytes);
            _stack.Push(heapBytes);

            ReadOnlyMemory<byte> contextBytes = host.Converter.Serialize(ref _heap, context);
            EnsureNotEmptyOnSerialize(heapBytes);
            _stack.Push(contextBytes);

            await host.Storage.SaveStackAsync(flowId, ref _stack, cancellationToken);
            await awaiter.SuspendAsync(ref this, cancellationToken);
        }

        public async Task ResumeAsync(IDTaskScope scope, DTask resultTask, CancellationToken cancellationToken)
        {
            _stack = await host.Storage.LoadStackAsync(flowId, cancellationToken);

            ReadOnlyMemory<byte> contextBytes = await _stack.PopAsync(cancellationToken);
            EnsureNotEmptyOnDeserialize(contextBytes);

            ReadOnlyMemory<byte> heapBytes = await _stack.PopAsync(cancellationToken);
            EnsureNotEmptyOnDeserialize(heapBytes);

            _heap = host.Converter.DeserializeHeap(flowId, scope, heapBytes.Span);

            DTask.DAwaiter awaiter = default;
            bool hasAwaiter = false;
            while (true)
            {
                ReadOnlyMemory<byte> stateMachineBytes = await _stack.PopAsync(cancellationToken);
                if (stateMachineBytes.IsEmpty)
                    break;

                resultTask = host.Converter.DeserializeStateMachine(flowId, ref _heap, stateMachineBytes.Span, resultTask);
                awaiter = resultTask.GetDAwaiter();

                if (!await awaiter.IsCompletedAsync())
                {
                    awaiter.SaveState(ref this);

                    ReadOnlyMemory<byte> bytes = host.Converter.SerializeHeap(ref _heap);
                    EnsureNotEmptyOnSerialize(bytes);
                    _stack.Push(bytes);

                    _stack.Push(contextBytes);

                    await host.Storage.SaveStackAsync(flowId, ref _stack, cancellationToken);
                    await awaiter.SuspendAsync(ref this, cancellationToken);
                    return;
                }

                hasAwaiter = true;
            }

            if (!hasAwaiter)
                throw CorruptedFlowData();

            _context = host.Converter.Deserialize<TFlowId, TContext>(flowId, ref _heap, contextBytes.Span);

            await awaiter.CompleteAsync(ref this, cancellationToken);
            await host.Storage.ClearStackAsync(flowId, ref _stack, cancellationToken);
        }

        void IStateHandler.SaveStateMachine<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo info)
        {
            ReadOnlyMemory<byte> bytes = host.Converter.SerializeStateMachine(ref _heap, ref stateMachine, info);
            EnsureNotEmptyOnDeserialize(bytes);
            _stack.Push(bytes);
        }

        readonly Task ISuspensionHandler.OnDelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            return host.OnDelayAsync(flowId, delay, cancellationToken);
        }

        readonly Task ISuspensionHandler.OnYieldAsync(CancellationToken cancellationToken)
        {
            return host.OnYieldAsync(flowId, cancellationToken);
        }

        readonly Task ISuspensionHandler.OnSuspendedAsync(ISuspensionCallback callback, CancellationToken cancellationToken)
        {
            return host.OnSuspendedAsync(flowId, callback, cancellationToken);
        }

        readonly Task ICompletionHandler.OnCompletedAsync(CancellationToken cancellationToken)
        {
            return host.OnCompletedAsync(flowId, _context, cancellationToken);
        }

        readonly Task ICompletionHandler.OnCompletedAsync<TResult>(TResult result, CancellationToken cancellationToken)
        {
            return host.OnCompletedAsync(flowId, _context, result, cancellationToken);
        }

        private readonly CorruptedDFlowException CorruptedFlowData() => new CorruptedDFlowException(flowId, $"Data relative to d-async flow '{flowId}' was missing or corrupted.");

        private readonly void EnsureNotEmptyOnDeserialize(ReadOnlyMemory<byte> bytes)
        {
            if (bytes.IsEmpty)
                throw CorruptedFlowData();
        }

        private static void EnsureNotEmptyOnSerialize(ReadOnlyMemory<byte> bytes)
        {
            if (bytes.IsEmpty)
                throw new InvalidOperationException("Serialization produced an empty sequence of bytes.");
        }
    }
}
