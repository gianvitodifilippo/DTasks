using DTasks.Hosting;
using System.Reflection;
using static DTasks.Inspection.InspectionFixtures;

namespace DTasks.Inspection;

public class InspectionTests
{
    private readonly StateMachineInspector _inspector;

    public InspectionTests()
    {
        _inspector = StateMachineInspector.Create(typeof(TestSuspender<>), typeof(TestResumer));
    }

    [Fact]
    public void Resumer_ShouldInvokeExpectedConstructorMethods()
    {
        // Arrange
        int expectedNumberOfCalls =
#if DEBUG
            7;
#else
            5;
#endif
        var resultTask = Substitute.For<DTask>();
        var constructor = Substitute.For<IStateMachineConstructor>();
        var resumer = (TestResumer)_inspector.GetResumer(StateMachineType);

        constructor
            .HandleState(Arg.Any<string>(), ref Arg.Any<int>())
            .Returns(call =>
            {
                call[1] = -1;
                return true;
            });

        constructor
            .HandleAwaiter("<>u__3")
            .Returns(true);

        // Act
        DTask task = resumer.Invoke(resultTask, constructor);

        // Assert
        constructor.ReceivedCalls().Should().HaveCount(expectedNumberOfCalls);
        Received.InOrder(() =>
        {
            constructor.HandleField("arg", ref Arg.Any<MyType>());
            constructor.HandleField(LocalFieldName, ref Arg.Any<string>());
#if DEBUG
            constructor.HandleField("<result>5__2", ref Arg.Any<int>());
            constructor.HandleField("<>s__3", ref Arg.Any<int>());
#endif
            constructor.HandleState("<>1__state", ref Arg.Any<int>());
            constructor.HandleAwaiter("<>u__1");
            constructor.HandleAwaiter("<>u__3");
        });
    }

    private void Suspender_ShouldInvokeExpectedDeconstructorMethods_Impl<TStateMachine>(TStateMachine stateMachine, MyType arg)
        where TStateMachine : notnull
    {
        // Arrange
        int expectedNumberOfCalls =
#if DEBUG
            6;
#else
            4;
#endif
        var info = Substitute.For<IStateMachineInfo>();
        var deconstructor = Substitute.For<IStateMachineDeconstructor>();
        var suspender = (TestSuspender<TStateMachine>)_inspector.GetSuspender(StateMachineType);

        info.SuspendedAwaiterType.Returns(typeof(DTask<int>.Awaiter));

        // Act
        suspender.Invoke(ref stateMachine, info, deconstructor);

        // Assert
        deconstructor.ReceivedCalls().Should().HaveCount(expectedNumberOfCalls);
        Received.InOrder(() =>
        {
            deconstructor.HandleField("arg", arg);
            deconstructor.HandleField(LocalFieldName, Arg.Any<string>());
#if DEBUG
            deconstructor.HandleField("<result>5__2", Arg.Any<int>());
            deconstructor.HandleField("<>s__3", Arg.Any<int>());
#endif
            deconstructor.HandleState("<>1__state", Arg.Any<int>());
            deconstructor.HandleAwaiter("<>u__3");
        });
    }

    [Fact]
    public void Suspender_ShouldInvokeExpectedDeconstructorMethods()
    {
        MyType myType = new();
        DTask task = new AsyncMethodContainer().Method(myType);

        FieldInfo stateMachineField = task.GetType().GetRequiredField(
            name: "_stateMachine",
            bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance);

        object? stateMachine = stateMachineField.GetValue(task);

        MethodInfo implementationMethod = typeof(InspectionTests).GetRequiredMethod(
            name: nameof(Suspender_ShouldInvokeExpectedDeconstructorMethods_Impl),
            bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance,
            parameterTypes: [Type.MakeGenericMethodParameter(0), typeof(MyType)]);

        implementationMethod = implementationMethod.MakeGenericMethod(StateMachineType);
        implementationMethod.Invoke(this, [stateMachine, myType]);
    }

    public delegate void TestSuspender<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo info, IStateMachineDeconstructor deconstructor);

    public delegate DTask TestResumer(DTask resultTask, IStateMachineConstructor constructor);

    public interface IStateMachineDeconstructor
    {
        void HandleAwaiter(string fieldName);
        void HandleField<TField>(string fieldName, TField field);

        void HandleState(string fieldName, int state);

        void HandleField(string fieldName, int field);
        void HandleField(string fieldName, string field);
        void HandleField(string fieldName, MyType field);
    }

    public interface IStateMachineConstructor
    {
        bool HandleAwaiter(string fieldName);
        bool HandleField<TField>(string fieldName, ref TField field);

        bool HandleState(string fieldName, ref int state);

        bool HandleField(string fieldName, ref int field);
        bool HandleField(string fieldName, ref string field);
        bool HandleField(string fieldName, ref MyType field);
    }
}
