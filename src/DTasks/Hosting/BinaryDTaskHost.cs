using DTasks.Serialization;
using DTasks.Storage;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DTasks.Hosting;

public abstract class BinaryDTaskHost<TFlowId, TStack, THeap>
    where TFlowId : notnull
    where TStack : notnull, IFlowStack
    where THeap : notnull, IFlowHeap
{
    private readonly IDTaskStorage<TStack> _storage;
    private readonly IDTaskConverter<THeap> _converter;

    public BinaryDTaskHost(IDTaskStorage<TStack> storage, IDTaskConverter<THeap> converter)
    {
        _storage = storage;
        _converter = converter;
    }

    protected abstract Task OnSuspendedAsync(TFlowId flowId, ISuspensionCallback callback, CancellationToken cancellationToken);

    protected abstract Task OnDelayAsync(TFlowId flowId, TimeSpan delay, CancellationToken cancellationToken);

    protected abstract Task OnYieldAsync(TFlowId flowId, CancellationToken cancellationToken);

    protected abstract Task OnCompletedAsync(TFlowId flowId, CancellationToken cancellationToken);

    protected abstract Task OnCompletedAsync<TResult>(TFlowId flowId, TResult result, CancellationToken cancellationToken);

    public Task SuspendAsync(TFlowId flowId, IDTaskScope scope, DTask.DAwaiter awaiter, CancellationToken cancellationToken = default)
    {
        FlowHandler handler = new(flowId, this);
        return handler.SuspendAsync(scope, awaiter, cancellationToken);
    }

    public Task SuspendAsync<TResult>(TFlowId flowId, IDTaskScope scope, DTask<TResult>.DAwaiter awaiter, CancellationToken cancellationToken = default)
    {
        FlowHandler handler = new(flowId, this);
        return handler.SuspendAsync(scope, Unsafe.As<DTask<TResult>.DAwaiter, DTask.DAwaiter>(ref awaiter), cancellationToken);
    }

    public Task ResumeAsync(TFlowId flowId, IDTaskScope scope, CancellationToken cancellationToken = default)
    {
        FlowHandler handler = new(flowId, this);
        return handler.ResumeAsync(scope, cancellationToken);
    }

    public Task ResumeAsync<TResult>(TFlowId flowId, TResult result, IDTaskScope scope, CancellationToken cancellationToken = default)
    {
        FlowHandler handler = new(flowId, this);
        return handler.ResumeAsync(result, scope, cancellationToken);
    }

    private struct FlowHandler(TFlowId flowId, BinaryDTaskHost<TFlowId, TStack, THeap> host) : IStateHandler, ISuspensionHandler, ICompletionHandler
    {
        private TStack _stack;
        private THeap _heap;

        public Task SuspendAsync(IDTaskScope scope, DTask.DAwaiter awaiter, CancellationToken cancellationToken)
        {
            AssertUninitialized();

            _stack = host._storage.CreateStack();
            _heap = host._converter.CreateHeap(scope);

            awaiter.SaveState(ref this);

            ReadOnlyMemory<byte> bytes = host._converter.SerializeHeap(ref _heap);
            _stack.PushHeap(bytes);

            host._converter.DisposeHeap(ref _heap);
            return SaveAndSuspendAsync(awaiter, cancellationToken);
        }

        private async Task SaveAndSuspendAsync(DTask.DAwaiter awaiter, CancellationToken cancellationToken)
        {
            await host._storage.SaveStackAsync(flowId, in _stack, cancellationToken);
            await awaiter.SuspendAsync(ref this, cancellationToken);
        }

        public Task ResumeAsync(IDTaskScope scope, CancellationToken cancellationToken)
        {
            return ResumeCoreAsync(DTask.CompletedTask, scope, cancellationToken);
        }

        public Task ResumeAsync<TResult>(TResult result, IDTaskScope scope, CancellationToken cancellationToken)
        {
            return ResumeCoreAsync(DTask.FromResult(result), scope, cancellationToken);
        }

        private async Task ResumeCoreAsync(DTask resultTask, IDTaskScope scope, CancellationToken cancellationToken)
        {
            AssertUninitialized();

            _stack = await host._storage.LoadStackAsync(flowId, cancellationToken);
            ReadOnlyMemory<byte> heapBytes = _stack.PopHeap();
            _heap = host._converter.DeserializeHeap(scope, heapBytes);

            DTask.DAwaiter awaiter;
            bool hasNext;
            do
            {
                ReadOnlyMemory<byte> stateMachineBytes = _stack.PopStateMachine(out hasNext);
                resultTask = host._converter.DeserializeStateMachine(ref _heap, stateMachineBytes, resultTask); // TODO: CancellationToken
                awaiter = resultTask.GetDAwaiter();

                if (!await awaiter.IsCompletedAsync())
                {
                    awaiter.SaveState(ref this);
                    await awaiter.SuspendAsync(ref this, cancellationToken);
                    return;
                }
            }
            while (hasNext);

            await awaiter.CompleteAsync(ref this, cancellationToken);
        }

        void IStateHandler.SaveStateMachine<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo info)
        {
            ReadOnlyMemory<byte> bytes = host._converter.SerializeStateMachine(ref _heap, ref stateMachine, info);
            _stack.PushStateMachine(bytes);
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
            return host.OnCompletedAsync(flowId, cancellationToken);
        }

        readonly Task ICompletionHandler.OnCompletedAsync<TResult>(TResult result, CancellationToken cancellationToken)
        {
            return host.OnCompletedAsync(flowId, result, cancellationToken);
        }

        [Conditional("DEBUG")]
        private readonly void AssertUninitialized()
        {
            Debug.Assert(_stack is null && _heap is null, "The handler cannot be reused.");
        }
    }
}
