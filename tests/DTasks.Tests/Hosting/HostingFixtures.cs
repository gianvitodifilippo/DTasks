using DTasks.Serialization;
using DTasks.Storage;

namespace DTasks.Hosting;

public static class HostingFixtures
{
    // The purpose of this class is to forward calls to public methods, which can be verified
    public abstract class TestBinaryDTaskHost(TestDTaskStorage storage, TestDTaskConverter converter) : BinaryDTaskHost<string, TestFlowStack, TestFlowHeap>(storage, converter)
    {
        public abstract Task OnDelayAsync_Public(string flowId, TimeSpan delay, CancellationToken cancellationToken);
        public abstract Task OnCompletedAsync_Public(string flowId, CancellationToken cancellationToken);
        public abstract Task OnCompletedAsync_Public<TResult>(string flowId, TResult result, CancellationToken cancellationToken);
        public abstract Task OnSuspendedAsync_Public(string flowId, ISuspensionCallback callback, CancellationToken cancellationToken);
        public abstract Task OnYieldAsync_Public(string flowId, CancellationToken cancellationToken);

        protected sealed override Task OnDelayAsync(string flowId, TimeSpan delay, CancellationToken cancellationToken) => OnDelayAsync_Public(flowId, delay, cancellationToken);
        protected sealed override Task OnCompletedAsync(string flowId, CancellationToken cancellationToken) => OnCompletedAsync_Public(flowId, cancellationToken);
        protected sealed override Task OnCompletedAsync<TResult>(string flowId, TResult result, CancellationToken cancellationToken) => OnCompletedAsync_Public(flowId, result, cancellationToken);
        protected sealed override Task OnSuspendedAsync(string flowId, ISuspensionCallback callback, CancellationToken cancellationToken) => OnSuspendedAsync_Public(flowId, callback, cancellationToken);
        protected sealed override Task OnYieldAsync(string flowId, CancellationToken cancellationToken) => OnYieldAsync_Public(flowId, cancellationToken);
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

    public class EquatableArray<T>(T[] array) : IEquatable<EquatableArray<T>>
    {
        private readonly T[] _array = array;

        public bool Equals(EquatableArray<T>? other) => other is not null && _array.SequenceEqual(other._array);

        public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

        public override int GetHashCode() => _array.Length;

        public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

        public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !(left == right);

        public static implicit operator EquatableArray<T>(ReadOnlySpan<T> span) => new(span.ToArray());

        public static implicit operator EquatableArray<T>(ReadOnlyMemory<T> memory) => new(memory.ToArray());

        public static implicit operator EquatableArray<T>(T[] array) => new(array);

        public static implicit operator ReadOnlySpan<T>(EquatableArray<T> instance) => instance._array;

        public static implicit operator ReadOnlyMemory<T>(EquatableArray<T> instance) => instance._array;
    }

    public abstract class TestFlowStack : IFlowStack
    {
        public abstract ValueTask<EquatableArray<byte>> PopAsync(CancellationToken cancellationToken);

        public abstract void Push(EquatableArray<byte> bytes);

        async ValueTask<ReadOnlyMemory<byte>> IFlowStack.PopAsync(CancellationToken cancellationToken)
            => await PopAsync(cancellationToken);

        void IFlowStack.Push(ReadOnlyMemory<byte> bytes)
            => Push(bytes);
    }

    public abstract class TestFlowHeap;

    public abstract class TestDTaskStorage : IDTaskStorage<TestFlowStack>
    {
        public abstract TestFlowStack CreateStack();

        public abstract ValueTask<TestFlowStack> LoadStackAsync<TFlowId>(TFlowId flowId, CancellationToken cancellationToken)
            where TFlowId : notnull;

        public abstract Task SaveStackAsync<TFlowId>(TFlowId flowId, ref TestFlowStack stack, CancellationToken cancellationToken)
            where TFlowId : notnull;
    }

    public abstract class TestDTaskConverter : IDTaskConverter<TestFlowHeap>
    {
        public abstract TestFlowHeap CreateHeap(IDTaskScope scope);

        public abstract TestFlowHeap DeserializeHeap<TFlowId>(TFlowId flowId, IDTaskScope scope, EquatableArray<byte> bytes)
            where TFlowId : notnull;

        public abstract DTask DeserializeStateMachine<TFlowId>(TFlowId flowId, ref TestFlowHeap heap, EquatableArray<byte> bytes, DTask resultTask)
            where TFlowId : notnull;

        public abstract EquatableArray<byte> SerializeHeap(ref TestFlowHeap heap);

        public abstract EquatableArray<byte> SerializeStateMachine<TStateMachine>(ref TestFlowHeap heap, ref TStateMachine stateMachine, IStateMachineInfo info)
            where TStateMachine : notnull;

        TestFlowHeap IDTaskConverter<TestFlowHeap>.DeserializeHeap<TFlowId>(TFlowId flowId, IDTaskScope scope, ReadOnlySpan<byte> bytes)
            => DeserializeHeap(flowId, scope, bytes);

        DTask IDTaskConverter<TestFlowHeap>.DeserializeStateMachine<TFlowId>(TFlowId flowId, ref TestFlowHeap heap, ReadOnlySpan<byte> bytes, DTask resultTask)
            => DeserializeStateMachine(flowId, ref heap, bytes, resultTask);

        ReadOnlyMemory<byte> IDTaskConverter<TestFlowHeap>.SerializeHeap(ref TestFlowHeap heap)
            => SerializeHeap(ref heap);

        ReadOnlyMemory<byte> IDTaskConverter<TestFlowHeap>.SerializeStateMachine<TStateMachine>(ref TestFlowHeap heap, ref TStateMachine stateMachine, IStateMachineInfo info)
            => SerializeStateMachine(ref heap, ref stateMachine, info);
    }
}
