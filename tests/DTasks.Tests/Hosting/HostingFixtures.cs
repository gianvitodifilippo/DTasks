using DTasks.Inspection;
using DTasks.Serialization;
using DTasks.Storage;
using System.Runtime.InteropServices;
using System.Text;
using Xunit.Sdk;
using static DTasks.Hosting.DAsyncFlowTests;

namespace DTasks.Hosting;

public static class HostingFixtures
{
    // The purpose of this class is to forward calls to public methods, which can be verified
    public abstract class TestBinaryDTaskHost(TestDTaskStorage storage, TestDTaskConverter converter) : BinaryDTaskHost<TestFlowContext, TestFlowStack, TestFlowHeap>
    {
        private static readonly byte[] s_contextBytes = Encoding.UTF8.GetBytes("context");

        protected sealed override IDTaskStorage<TestFlowStack> Storage => storage;

        protected sealed override IDTaskConverter<TestFlowHeap> Converter => converter;

        protected sealed override Task OnCallbackAsync<TCallback>(FlowId id, IDTaskScope scope, TCallback callback, CancellationToken cancellationToken)
            => OnCallbackAsync_Public(id, scope, callback, cancellationToken);

        protected sealed override Task OnCallbackAsync<TState, TCallback>(FlowId id, IDTaskScope scope, TState state, TCallback callback, CancellationToken cancellationToken)
            => OnCallbackAsync_Public(id, scope, state, callback, cancellationToken);

        protected sealed override Task OnDelayAsync(FlowId id, IDTaskScope scope, TimeSpan delay, CancellationToken cancellationToken)
            => OnDelayAsync_Public(id, scope, delay, cancellationToken);

        protected sealed override Task OnYieldAsync(FlowId id, IDTaskScope scope, CancellationToken cancellationToken)
            => OnYieldAsync_Public(id, scope, cancellationToken);

        protected sealed override Task OnCompletedAsync(FlowId id, TestFlowContext context, CancellationToken cancellationToken)
            => OnCompletedAsync_Public(id, context, cancellationToken);

        protected sealed override Task OnCompletedAsync<TResult>(FlowId id, TestFlowContext context, TResult result, CancellationToken cancellationToken)
            => OnCompletedAsync_Public(id, context, result, cancellationToken);

        protected sealed override async Task OnWhenAllAsync(FlowId id, IDTaskScope scope, IEnumerable<DTask> tasks, CancellationToken cancellationToken)
        {
            await OnWhenAllAsync_Public(id, scope, tasks, cancellationToken);
            await base.OnWhenAllAsync(id, scope, tasks, cancellationToken);
        }

        // Verifiable methods

        public abstract Task OnCallbackAsync_Public<TCallback>(FlowId id, IDTaskScope scope, TCallback callback, CancellationToken cancellationToken)
            where TCallback : ISuspensionCallback;

        public abstract Task OnCallbackAsync_Public<TState, TCallback>(FlowId id, IDTaskScope scope, TState state, TCallback callback, CancellationToken cancellationToken)
            where TCallback : ISuspensionCallback<TState>;

        public abstract Task OnDelayAsync_Public(FlowId id, IDTaskScope scope, TimeSpan delay, CancellationToken cancellationToken);

        public abstract Task OnYieldAsync_Public(FlowId id, IDTaskScope scope, CancellationToken cancellationToken);

        public abstract Task OnWhenAllAsync_Public(FlowId id, IDTaskScope scope, IEnumerable<DTask> tasks, CancellationToken cancellationToken);

        public abstract Task OnCompletedAsync_Public(FlowId id, TestFlowContext context, CancellationToken cancellationToken);

        public abstract Task OnCompletedAsync_Public<TResult>(FlowId id, TestFlowContext context, TResult result, CancellationToken cancellationToken);
    }

    // The following classes are preconfigured for tests but also allow creating a substitute for to verify method calls.
    // The XXX_Public methods are there as a workaround to https://github.com/nsubstitute/NSubstitute/issues/787

    public abstract class TestSuspendedDTask(bool isStateful) : DTask
    {
        internal sealed override DTaskStatus Status => DTaskStatus.Suspended;

        internal sealed override Task<bool> UnderlyingTask => Task.FromResult(false);

        internal sealed override bool IsStateful => isStateful;

        public abstract void SaveState_Public<THandler>(THandler handler)
            where THandler : IStateHandler;

        public abstract Task SuspendAsync_Public<THandler>(THandler handler, CancellationToken cancellationToken)
            where THandler : ISuspensionHandler;

        internal sealed override void SaveState<THandler>(ref THandler handler)
        {
            SaveState_Public(handler);

            if (!isStateful)
                return;

            Arg.AnyType stateMachine = Substitute.For<Arg.AnyType>();
            handler.SaveStateMachine(ref stateMachine, Substitute.For<IStateMachineInfo>());
        }

        internal sealed override Task SuspendAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
        {
            return SuspendAsync_Public(handler, cancellationToken);
        }

        internal sealed override Task CompleteAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
        {
            throw FailException.ForFailure($"{nameof(CompleteAsync)} should not be called on a suspended DTask.");
        }
    }

    public abstract class TestCompletedDTask : DTask
    {
        internal sealed override DTaskStatus Status => DTaskStatus.RanToCompletion;

        internal sealed override Task<bool> UnderlyingTask => Task.FromResult(true);

        public abstract Task CompleteAsync_Public<THandler>(THandler handler, CancellationToken cancellationToken)
            where THandler : ICompletionHandler;

        internal sealed override void SaveState<THandler>(ref THandler handler)
        {
            throw FailException.ForFailure($"{nameof(SaveState)} should not be called on a completed DTask.");
        }

        internal sealed override Task SuspendAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
        {
            throw FailException.ForFailure($"{nameof(SuspendAsync)} should not be called on a completed DTask.");
        }

        internal sealed override Task CompleteAsync<THandler>(ref THandler handler, CancellationToken cancellationToken)
        {
            return CompleteAsync_Public(handler, cancellationToken);
        }
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

    public abstract class TestFlowHeap : IFlowHeap
    {
        public abstract uint StackCount { get; set; }
    }

    public abstract class TestDTaskStorage : IDTaskStorage<TestFlowStack>
    {
        public abstract Task ClearStackAsync(FlowId flowId, ref TestFlowStack stack, CancellationToken cancellationToken = default);
        
        public abstract Task ClearValueAsync(FlowId flowId, CancellationToken cancellationToken = default);
        
        public abstract TestFlowStack CreateStack();
        
        public abstract ValueTask<TestFlowStack> LoadStackAsync(FlowId flowId, CancellationToken cancellationToken = default);
        
        public abstract ValueTask<EquatableArray<byte>> LoadValueAsync(FlowId flowId, CancellationToken cancellationToken = default);
        
        public abstract Task SaveStackAsync(FlowId flowId, ref TestFlowStack stack, CancellationToken cancellationToken = default);
        
        public abstract Task SaveValueAsync(FlowId flowId, EquatableArray<byte> bytes, CancellationToken cancellationToken = default);

        async ValueTask<ReadOnlyMemory<byte>> IDTaskStorage<TestFlowStack>.LoadValueAsync(FlowId flowId, CancellationToken cancellationToken)
            => await LoadValueAsync(flowId, cancellationToken);

        Task IDTaskStorage<TestFlowStack>.SaveValueAsync(FlowId flowId, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
            => SaveValueAsync(flowId, bytes, cancellationToken);
    }

    public abstract class TestDTaskConverter : IDTaskConverter<TestFlowHeap>
    {
        public abstract TestFlowHeap CreateHeap(IDTaskScope scope);
        
        public abstract T Deserialize<T>(EquatableArray<byte> bytes);
        
        public abstract TestFlowHeap DeserializeHeap(IDTaskScope scope, EquatableArray<byte> bytes);
        
        public abstract DTask DeserializeStateMachine(ref TestFlowHeap heap, EquatableArray<byte> bytes, DTask resultTask);
        
        public abstract EquatableArray<byte> Serialize<T>(T value);
        
        public abstract EquatableArray<byte> SerializeHeap(ref TestFlowHeap heap);
        
        public abstract EquatableArray<byte> SerializeStateMachine<TStateMachine>(ref TestFlowHeap heap, ref TStateMachine stateMachine, IStateMachineInfo info)
            where TStateMachine : notnull;

        T IDTaskConverter<TestFlowHeap>.Deserialize<T>(ReadOnlySpan<byte> bytes)
            => Deserialize<T>(bytes);

        TestFlowHeap IDTaskConverter<TestFlowHeap>.DeserializeHeap(IDTaskScope scope, ReadOnlySpan<byte> bytes)
            => DeserializeHeap(scope, bytes);

        DTask IDTaskConverter<TestFlowHeap>.DeserializeStateMachine(ref TestFlowHeap heap, ReadOnlySpan<byte> bytes, DTask resultTask)
            => DeserializeStateMachine(ref heap, bytes, resultTask);

        ReadOnlyMemory<byte> IDTaskConverter<TestFlowHeap>.Serialize<T>(T value)
            => Serialize(value);

        ReadOnlyMemory<byte> IDTaskConverter<TestFlowHeap>.SerializeHeap(ref TestFlowHeap heap)
            => SerializeHeap(ref heap);

        ReadOnlyMemory<byte> IDTaskConverter<TestFlowHeap>.SerializeStateMachine<TStateMachine>(ref TestFlowHeap heap, ref TStateMachine stateMachine, IStateMachineInfo info)
            => SerializeStateMachine(ref heap, ref stateMachine, info);
    }

    // The following classes are fakes with minimal logic

    public class FakeFlowStack : TestFlowStack
    {
        private static readonly EquatableArray<byte> s_emptyArray = new([]);

        private readonly Stack<EquatableArray<byte>> _stack = [];

        public override ValueTask<EquatableArray<byte>> PopAsync(CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(_stack.TryPop(out EquatableArray<byte>? bytes)
                ? bytes
                : s_emptyArray);
        }

        public override void Push(EquatableArray<byte> bytes)
        {
            _stack.Push(bytes);
        }
    }

    public class FakeFlowHeap : TestFlowHeap
    {
        public override uint StackCount { get; set; }
    }

    public class FakeDTaskStorage : TestDTaskStorage
    {
        private readonly Dictionary<FlowId, TestFlowStack> _stacks = [];
        private readonly Dictionary<FlowId, EquatableArray<byte>> _values = [];

        public override TestFlowStack CreateStack()
        {
            return new FakeFlowStack();
        }

        public override ValueTask<TestFlowStack> LoadStackAsync(FlowId flowId, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(_stacks[flowId]);
        }

        public override Task SaveStackAsync(FlowId flowId, ref TestFlowStack stack, CancellationToken cancellationToken = default)
        {
            _stacks[flowId] = stack;
            return Task.CompletedTask;
        }

        public override Task ClearStackAsync(FlowId flowId, ref TestFlowStack stack, CancellationToken cancellationToken = default)
        {
            if (!_stacks.Remove(flowId))
                FailException.ForFailure("Invalid flow id");

            return Task.CompletedTask;
        }

        public override ValueTask<EquatableArray<byte>> LoadValueAsync(FlowId flowId, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(_values[flowId]);
        }

        public override Task SaveValueAsync(FlowId flowId, EquatableArray<byte> bytes, CancellationToken cancellationToken = default)
        {
            _values[flowId] = bytes;
            return Task.CompletedTask;
        }

        public override Task ClearValueAsync(FlowId flowId, CancellationToken cancellationToken = default)
        {
            if (!_values.Remove(flowId))
                FailException.ForFailure("Invalid flow id");

            return Task.CompletedTask;
        }
    }

    public class FakeDTaskConverter : TestDTaskConverter
    {
        private static readonly EquatableArray<byte> s_heapBytes = Encoding.UTF8.GetBytes("heap");

        private readonly StateMachineInspector _inspector = StateMachineInspector.Create(typeof(TestSuspender<>), typeof(TestResumer));
        private readonly Dictionary<int, Dictionary<string, object?>> _stateMachines = [];
        private readonly Dictionary<int, object?> _values = [];
        private TestFlowHeap? _heap;
        private int _counter = 0;

        public override TestFlowHeap CreateHeap(IDTaskScope scope)
        {
            if (_heap is not null)
                throw FailException.ForFailure("Heap was already created.");

            _heap = new FakeFlowHeap();
            return _heap;
        }

        public override TestFlowHeap DeserializeHeap(IDTaskScope scope, EquatableArray<byte> bytes)
        {
            if (bytes != s_heapBytes)
                throw FailException.ForFailure("Invalid heap bytes.");

            if (_heap is null)
                throw FailException.ForFailure("Heap was not created.");

            return _heap;
        }

        public override DTask DeserializeStateMachine(ref TestFlowHeap heap, EquatableArray<byte> bytes, DTask resultTask)
        {
            int id = MemoryMarshal.Read<int>(bytes);
            if (!_stateMachines.Remove(id, out Dictionary<string, object?>? stateMachineDictionary))
                throw FailException.ForFailure("Invalid state machine bytes.");

            var stateMachineType = (Type)stateMachineDictionary["$type"]!;
            var constructor = new StateMachineConstructor(stateMachineDictionary);

            var resumer = (TestResumer)_inspector.GetResumer(stateMachineType);
            return resumer(resultTask, constructor);
        }

        public override T Deserialize<T>(EquatableArray<byte> bytes)
        {
            int id = MemoryMarshal.Read<int>(bytes);
            if (!_values.Remove(id, out object? value))
                throw FailException.ForFailure("Invalid value bytes.");

            return (T)value!;
        }

        public override EquatableArray<byte> SerializeHeap(ref TestFlowHeap heap)
        {
            if (heap != _heap)
                throw FailException.ForFailure("Invalid heap.");

            return s_heapBytes;
        }

        public override EquatableArray<byte> SerializeStateMachine<TStateMachine>(ref TestFlowHeap heap, ref TStateMachine stateMachine, IStateMachineInfo info)
        {
            var stateMachineDictionary = new Dictionary<string, object?>() { ["$type"] = typeof(TStateMachine) };
            var deconstructor = new StateMachineDeconstructor(stateMachineDictionary);

            var suspender = (TestSuspender<TStateMachine>)_inspector.GetSuspender(typeof(TStateMachine));
            suspender(ref stateMachine, info, deconstructor);

            int id = ++_counter;
            byte[] bytes = new byte[4];
            MemoryMarshal.Write(bytes, id);
            _stateMachines.Add(id, stateMachineDictionary);

            return bytes;
        }

        public override EquatableArray<byte> Serialize<T>(T value)
        {
            int id = ++_counter;
            byte[] bytes = new byte[4];
            MemoryMarshal.Write(bytes, id);
            _values.Add(id, value);

            return bytes;
        }
    }
}
