namespace DTasks.Hosting;

public partial class DAsyncFlowTests
{
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
