using DTasks.Serialization;
using DTasks.Storage;

namespace DTasks.Hosting;

public static class HostingFixtures
{
    // The purpose of this class is to forward calls to public methods, which can be verified
    public abstract class TestBinaryDTaskHost(TestDTaskStorage storage, TestDTaskConverter converter) : BinaryDTaskHost<Guid, TestFlowStack, TestFlowHeap>(storage, converter)
    {
        public abstract Task OnDelayAsync_Public(Guid flowId, TimeSpan delay, CancellationToken cancellationToken);
        public abstract Task OnCompletedAsync_Public(Guid flowId, CancellationToken cancellationToken);
        public abstract Task OnCompletedAsync_Public<TResult>(Guid flowId, TResult result, CancellationToken cancellationToken);
        public abstract Task OnSuspendedAsync_Public(Guid flowId, ISuspensionCallback callback, CancellationToken cancellationToken);
        public abstract Task OnYieldAsync_Public(Guid flowId, CancellationToken cancellationToken);

        protected sealed override Task OnDelayAsync(Guid flowId, TimeSpan delay, CancellationToken cancellationToken) => OnDelayAsync_Public(flowId, delay, cancellationToken);
        protected sealed override Task OnCompletedAsync(Guid flowId, CancellationToken cancellationToken) => OnCompletedAsync_Public(flowId, cancellationToken);
        protected sealed override Task OnCompletedAsync<TResult>(Guid flowId, TResult result, CancellationToken cancellationToken) => OnCompletedAsync_Public(flowId, result, cancellationToken);
        protected sealed override Task OnSuspendedAsync(Guid flowId, ISuspensionCallback callback, CancellationToken cancellationToken) => OnSuspendedAsync_Public(flowId, callback, cancellationToken);
        protected sealed override Task OnYieldAsync(Guid flowId, CancellationToken cancellationToken) => OnYieldAsync_Public(flowId, cancellationToken);
    }

    // The following classes are preconfigured for tests but also allow creating a substitute for to verify method calls.

    public abstract class TestSuspendedDTask : DTask
    {
        internal sealed override DTaskStatus Status => DTaskStatus.Suspended;

        internal sealed override Task<bool> UnderlyingTask => Task.FromResult(false);
    }

    public abstract class TestCompletedDTask : DTask
    {
        internal sealed override DTaskStatus Status => DTaskStatus.RanToCompletion;

        internal sealed override Task<bool> UnderlyingTask => Task.FromResult(true);
    }

    // The following classes allow configuring substitutes when methods accepts or returns ReadOnlySpan.
    // Some of them do not contain such methods, but they are included for consistency.

    public class TestReadOnlySpan<T>(T[] array) : IEquatable<TestReadOnlySpan<T>>
    {
        private readonly T[] _array = array;

        public bool Equals(TestReadOnlySpan<T>? other) => other is not null && _array.SequenceEqual(other._array);

        public override bool Equals(object? obj) => obj is TestReadOnlySpan<T> other && Equals(other);

        public override int GetHashCode() => _array.Length;

        public static implicit operator TestReadOnlySpan<T>(ReadOnlySpan<T> span) => new(span.ToArray());

        public static implicit operator TestReadOnlySpan<T>(T[] array) => new(array);

        public static implicit operator ReadOnlySpan<T>(TestReadOnlySpan<T> instance) => instance._array;
    }

    public abstract class TestFlowStack : IFlowStack
    {
        public abstract TestReadOnlySpan<byte> PopHeap();

        public abstract TestReadOnlySpan<byte> PopStateMachine(out bool hasNext);

        public abstract void PushHeap(TestReadOnlySpan<byte> bytes);

        public abstract void PushStateMachine(TestReadOnlySpan<byte> bytes);

        ReadOnlySpan<byte> IFlowStack.PopHeap() => PopHeap();

        ReadOnlySpan<byte> IFlowStack.PopStateMachine(out bool hasNext) => PopStateMachine(out hasNext);

        void IFlowStack.PushHeap(ReadOnlySpan<byte> bytes) => PushHeap(bytes);

        void IFlowStack.PushStateMachine(ReadOnlySpan<byte> bytes) => PushStateMachine(bytes);
    }

    public abstract class TestFlowHeap : IFlowHeap
    {
        public abstract void AddHostReference<TToken>(object reference, TToken token);
    }

    public abstract class TestDTaskStorage : IDTaskStorage<TestFlowStack>
    {
        public abstract TestFlowStack CreateStack();

        public abstract Task<TestFlowStack> LoadStackAsync<TFlowId>(TFlowId flowId, CancellationToken cancellationToken) where TFlowId : notnull;

        public abstract Task SaveStackAsync<TFlowId>(TFlowId flowId, ref TestFlowStack stack, CancellationToken cancellationToken) where TFlowId : notnull;
    }

    public abstract class TestDTaskConverter : IDTaskConverter<TestFlowHeap>
    {
        public abstract TestFlowHeap CreateHeap();

        public abstract TestFlowHeap DeserializeHeap(IResumptionScope scope, TestReadOnlySpan<byte> bytes);

        public abstract DTask DeserializeStateMachine(ref TestFlowHeap heap, TestReadOnlySpan<byte> bytes, DTask resultTask);

        public abstract TestReadOnlySpan<byte> SerializeHeap(ref TestFlowHeap heap);

        public abstract TestReadOnlySpan<byte> SerializeStateMachine<TStateMachine>(ref TestFlowHeap heap, ref TStateMachine stateMachine, IStateMachineInfo info)
            where TStateMachine : notnull;

        TestFlowHeap IDTaskConverter<TestFlowHeap>.DeserializeHeap(IResumptionScope scope, ReadOnlySpan<byte> bytes)
            => DeserializeHeap(scope, bytes);

        DTask IDTaskConverter<TestFlowHeap>.DeserializeStateMachine(ref TestFlowHeap heap, ReadOnlySpan<byte> bytes, DTask resultTask)
            => DeserializeStateMachine(ref heap, bytes, resultTask);

        ReadOnlySpan<byte> IDTaskConverter<TestFlowHeap>.SerializeHeap(ref TestFlowHeap heap)
            => SerializeHeap(ref heap);

        ReadOnlySpan<byte> IDTaskConverter<TestFlowHeap>.SerializeStateMachine<TStateMachine>(ref TestFlowHeap heap, ref TStateMachine stateMachine, IStateMachineInfo info)
            => SerializeStateMachine(ref heap, ref stateMachine, info);
    }
}
