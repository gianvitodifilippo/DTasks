using DTasks.Serialization;
using DTasks.Storage;
using System.Text;

namespace DTasks.Hosting;

public static class HostingFixtures
{
    // The purpose of this class is to forward calls to public methods, which can be verified
    public abstract class TestBinaryDTaskHost(TestDTaskStorage storage, TestDTaskConverter converter) : BinaryDTaskHost<TestFlowContext, TestFlowStack, TestFlowHeap>
    {
        private static readonly byte[] s_contextBytes = Encoding.UTF8.GetBytes("context");

        protected sealed override IDTaskStorage<TestFlowStack> Storage => storage;

        protected sealed override IDTaskConverter<TestFlowHeap> Converter => converter;

        public abstract Task OnDelayAsync_Public(FlowId flowId, TimeSpan delay, CancellationToken cancellationToken);
        public abstract Task OnCompletedAsync_Public(FlowId flowId, TestFlowContext context, CancellationToken cancellationToken);
        public abstract Task OnCompletedAsync_Public<TResult>(FlowId flowId, TestFlowContext context, TResult result, CancellationToken cancellationToken);
        public abstract Task OnSuspendedAsync_Public(FlowId flowId, ISuspensionCallback callback, CancellationToken cancellationToken);
        public abstract Task OnYieldAsync_Public(FlowId flowId, CancellationToken cancellationToken);

        protected sealed override Task OnDelayAsync(FlowId flowId, TimeSpan delay, CancellationToken cancellationToken) => OnDelayAsync_Public(flowId, delay, cancellationToken);
        protected sealed override Task OnCompletedAsync(FlowId flowId, TestFlowContext context, CancellationToken cancellationToken) => OnCompletedAsync_Public(flowId, context, cancellationToken);
        protected sealed override Task OnCompletedAsync<TResult>(FlowId flowId, TestFlowContext context, TResult result, CancellationToken cancellationToken) => OnCompletedAsync_Public(flowId, context, result, cancellationToken);
        protected sealed override Task OnCallbackAsync(FlowId flowId, ISuspensionCallback callback, CancellationToken cancellationToken) => OnSuspendedAsync_Public(flowId, callback, cancellationToken);
        protected sealed override Task OnYieldAsync(FlowId flowId, CancellationToken cancellationToken) => OnYieldAsync_Public(flowId, cancellationToken);
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

    public sealed class TestFlowContext;

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

        public abstract Task ClearStackAsync<TFlowId>(TFlowId flowId, ref TestFlowStack stack, CancellationToken cancellationToken = default) where TFlowId : notnull;
    }

    public abstract class TestDTaskConverter : IDTaskConverter<TestFlowHeap>
    {
        public abstract TestFlowHeap CreateHeap(IDTaskScope scope);

        public abstract TestFlowHeap DeserializeHeap<TFlowId>(TFlowId flowId, IDTaskScope scope, EquatableArray<byte> bytes)
            where TFlowId : notnull;

        public abstract DTask DeserializeStateMachine<TFlowId>(TFlowId flowId, ref TestFlowHeap heap, EquatableArray<byte> bytes, DTask resultTask)
            where TFlowId : notnull;

        public abstract T Deserialize<TFlowId, T>(TFlowId flowId, ref TestFlowHeap heap, EquatableArray<byte> bytes)
            where TFlowId : notnull;

        public abstract EquatableArray<byte> SerializeHeap(ref TestFlowHeap heap);

        public abstract EquatableArray<byte> SerializeStateMachine<TStateMachine>(ref TestFlowHeap heap, ref TStateMachine stateMachine, IStateMachineInfo info)
            where TStateMachine : notnull;

        public abstract EquatableArray<byte> Serialize<T>(ref TestFlowHeap heap, T value);

        TestFlowHeap IDTaskConverter<TestFlowHeap>.DeserializeHeap<TFlowId>(TFlowId flowId, IDTaskScope scope, ReadOnlySpan<byte> bytes)
            => DeserializeHeap(flowId, scope, bytes);

        DTask IDTaskConverter<TestFlowHeap>.DeserializeStateMachine<TFlowId>(TFlowId flowId, ref TestFlowHeap heap, ReadOnlySpan<byte> bytes, DTask resultTask)
            => DeserializeStateMachine(flowId, ref heap, bytes, resultTask);

        T IDTaskConverter<TestFlowHeap>.Deserialize<TFlowId, T>(TFlowId flowId, ref TestFlowHeap heap, ReadOnlySpan<byte> bytes)
            => Deserialize<TFlowId, T>(flowId, ref heap, bytes);

        ReadOnlyMemory<byte> IDTaskConverter<TestFlowHeap>.SerializeHeap(ref TestFlowHeap heap)
            => SerializeHeap(ref heap);

        ReadOnlyMemory<byte> IDTaskConverter<TestFlowHeap>.SerializeStateMachine<TStateMachine>(ref TestFlowHeap heap, ref TStateMachine stateMachine, IStateMachineInfo info)
            => SerializeStateMachine(ref heap, ref stateMachine, info);

        ReadOnlyMemory<byte> IDTaskConverter<TestFlowHeap>.Serialize<T>(ref TestFlowHeap heap, T value)
            => Serialize(ref heap, value);
    }
}
