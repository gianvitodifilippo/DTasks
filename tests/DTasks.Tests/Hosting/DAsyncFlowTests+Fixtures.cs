using DTasks.Inspection;
using System.Runtime.InteropServices;
using Xunit.Sdk;
using static DTasks.Hosting.HostingFixtures;

namespace DTasks.Hosting;

public partial class DAsyncFlowTests
{
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

    public class FakeDTaskStorage : TestDTaskStorage
    {
        private readonly Dictionary<object, TestFlowStack> _stacks = [];

        public IReadOnlyDictionary<object, TestFlowStack> Stacks => _stacks;

        public override TestFlowStack CreateStack()
        {
            return new FakeFlowStack();
        }

        public override ValueTask<TestFlowStack> LoadStackAsync<TFlowId>(TFlowId flowId, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(_stacks[flowId]);
        }

        public override Task SaveStackAsync<TFlowId>(TFlowId flowId, ref TestFlowStack stack, CancellationToken cancellationToken)
        {
            _stacks[flowId] = stack;
            return Task.CompletedTask;
        }
    }

    public class FakeDTaskConverter : TestDTaskConverter
    {
        private static readonly EquatableArray<byte> s_heapBytes = new byte[] { 0 };

        private readonly StateMachineInspector _inspector = StateMachineInspector.Create(typeof(TestSuspender<>), typeof(TestResumer));
        private readonly Dictionary<int, Dictionary<string, object?>> _stateMachines = [];
        private int _counter = 0;

        public override TestFlowHeap CreateHeap(IDTaskScope scope)
        {
            return Substitute.For<TestFlowHeap>();
        }

        public override TestFlowHeap DeserializeHeap<TFlowId>(TFlowId flowId, IDTaskScope scope, EquatableArray<byte> bytes)
        {
            if (bytes != s_heapBytes)
                throw FailException.ForFailure("Invalid heap bytes.");

            return Substitute.For<TestFlowHeap>();
        }

        public override DTask DeserializeStateMachine<TFlowId>(TFlowId flowId, ref TestFlowHeap heap, EquatableArray<byte> bytes, DTask resultTask)
        {
            int id = MemoryMarshal.Read<int>(bytes);
            if (!_stateMachines.Remove(id, out Dictionary<string, object?>? stateMachineDictionary))
                throw FailException.ForFailure("Invalid state machine bytes.");

            var stateMachineType = (Type)stateMachineDictionary["$type"]!;
            var constructor = new StateMachineConstructor(stateMachineDictionary);

            var resumer = (TestResumer)_inspector.GetResumer(stateMachineType);
            return resumer(resultTask, constructor);
        }

        public override EquatableArray<byte> SerializeHeap(ref TestFlowHeap heap)
        {
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
    }

    public delegate void TestSuspender<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo info, StateMachineDeconstructor deconstructor);

    public delegate DTask TestResumer(DTask resultTask, StateMachineConstructor constructor);

    public class StateMachineConstructor(Dictionary<string, object?> stateMachineDictionary)
    {
        public bool HandleField<TField>(string fieldName, ref TField? value)
        {
            value = (TField?)stateMachineDictionary[fieldName];
            return true;
        }

        public bool HandleAwaiter(string fieldName)
        {
            return stateMachineDictionary.ContainsKey(fieldName);
        }
    }

    public class StateMachineDeconstructor(Dictionary<string, object?> stateMachineDictionary)
    {
        public void HandleField<TField>(string fieldName, TField? value)
        {
            stateMachineDictionary[fieldName] = value;
        }

        public void HandleAwaiter(string fieldName)
        {
            stateMachineDictionary[fieldName] = null;
        }
    }
}
