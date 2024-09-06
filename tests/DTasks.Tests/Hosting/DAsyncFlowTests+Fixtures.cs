using DTasks.Inspection;
using System.Runtime.InteropServices;
using static DTasks.Hosting.HostingFixtures;

namespace DTasks.Hosting;

public partial class DAsyncFlowTests
{
    public class FakeFlowStack : TestFlowStack
    {
        private readonly Stack<EquatableArray<byte>> _stack = [];

        public override EquatableArray<byte> PopHeap() => Array.Empty<byte>();

        public override void PushHeap(EquatableArray<byte> bytes) { }

        public override EquatableArray<byte> PopStateMachine(out bool hasNext)
        {
            hasNext = _stack.Count > 1;
            return _stack.Pop();
        }

        public override void PushStateMachine(EquatableArray<byte> bytes)
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

        public override Task<TestFlowStack> LoadStackAsync<TFlowId>(TFlowId flowId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_stacks[flowId]);
        }

        public override Task SaveStackAsync<TFlowId>(TFlowId flowId, ref TestFlowStack stack, CancellationToken cancellationToken)
        {
            _stacks[flowId] = stack;
            return Task.CompletedTask;
        }
    }

    public class FakeDTaskConverter : TestDTaskConverter
    {
        private readonly StateMachineInspector _inspector = StateMachineInspector.Create(typeof(TestSuspender<>), typeof(TestResumer));
        private readonly Dictionary<int, Dictionary<string, object?>> _stateMachines = [];
        private int _counter = 0;

        public override TestFlowHeap CreateHeap(IDTaskScope scope)
        {
            return Substitute.For<TestFlowHeap>();
        }

        public override TestFlowHeap DeserializeHeap(IDTaskScope scope, EquatableArray<byte> bytes)
        {
            return Substitute.For<TestFlowHeap>();
        }

        public override DTask DeserializeStateMachine(ref TestFlowHeap heap, EquatableArray<byte> bytes, DTask resultTask)
        {
            int id = MemoryMarshal.Read<int>(bytes);
            if (!_stateMachines.Remove(id, out Dictionary<string, object?>? stateMachineDictionary))
                throw new InvalidOperationException();

            var stateMachineType = (Type)stateMachineDictionary["$type"]!;
            var constructor = new StateMachineConstructor(stateMachineDictionary);

            var resumer = (TestResumer)_inspector.GetResumer(stateMachineType);
            return resumer(resultTask, constructor);
        }

        public override EquatableArray<byte> SerializeHeap(ref TestFlowHeap heap)
        {
            return Array.Empty<byte>();
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
