using DTasks.Hosting;

namespace DTasks.Inspection;

public partial class SuspenderDescriptorTests
{
    public delegate void StructSuspender<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo info, StructDeconstructor deconstructor)
        where TStateMachine : notnull;

    public delegate void ByRefStructSuspender<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo info, ref StructDeconstructor deconstructor)
        where TStateMachine : notnull;

    public delegate void ClassSuspender<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo info, ClassDeconstructor deconstructor)
        where TStateMachine : notnull;

    public delegate void ByRefClassSuspender<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo info, ref ClassDeconstructor deconstructor)
        where TStateMachine : notnull;


    public delegate void SuspenderWithHandleStateMethod<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo info, DeconstructorWithHandleStateMethod deconstructor)
        where TStateMachine : notnull;

    public delegate void SuspenderWithSpecializedMethod<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo info, DeconstructorWithSpecializedMethod deconstructor)
        where TStateMachine : notnull;

    public delegate void SuspenderWithSpecializedMethodAndHandleStateMethod<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo info, DeconstructorWithSpecializedMethodAndHandleStateMethod deconstructor)
        where TStateMachine : notnull;


    public delegate void SuspenderWithoutHandleField<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo info, DeconstructorWithoutHandleField deconstructor)
        where TStateMachine : notnull;

    public delegate void SuspenderWithoutHandleAwaiter<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo info, DeconstructorWithoutHandleAwaiter deconstructor)
        where TStateMachine : notnull;


    public class DeconstructorWithoutHandleField
    {
        public void HandleAwaiter(string fieldName) => throw new NotImplementedException();
    }

    public class DeconstructorWithoutHandleAwaiter
    {
        public void HandleField<TField>(string fieldName, TField field) => throw new NotImplementedException();
    }

    public struct StructDeconstructor
    {
        public void HandleField<TField>(string fieldName, TField field) => throw new NotImplementedException();
        public void HandleAwaiter(string fieldName) => throw new NotImplementedException();
    }

    public class ClassDeconstructor
    {
        public void HandleField<TField>(string fieldName, TField field) => throw new NotImplementedException();
        public void HandleAwaiter(string fieldName) => throw new NotImplementedException();
    }

    public class DeconstructorWithHandleStateMethod
    {
        public void HandleField<TField>(string fieldName, TField field) => throw new NotImplementedException();
        public void HandleAwaiter(string fieldName) => throw new NotImplementedException();
        public void HandleState(string fieldName, int state) => throw new NotImplementedException();
    }

    public class DeconstructorWithSpecializedMethod
    {
        public void HandleField<TField>(string fieldName, TField field) => throw new NotImplementedException();
        public void HandleAwaiter(string fieldName) => throw new NotImplementedException();
        public void HandleField(string fieldName, int field) => throw new NotImplementedException();
    }

    public class DeconstructorWithSpecializedMethodAndHandleStateMethod
    {
        public void HandleField<TField>(string fieldName, TField field) => throw new NotImplementedException();
        public void HandleAwaiter(string fieldName) => throw new NotImplementedException();
        public void HandleField(string fieldName, int field) => throw new NotImplementedException();
        public void HandleState(string fieldName, int state) => throw new NotImplementedException();
    }
}
