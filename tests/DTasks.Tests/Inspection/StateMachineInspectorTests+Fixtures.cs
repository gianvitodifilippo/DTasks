using DTasks.CompilerServices;
using System.Linq.Expressions;
using System.Reflection;
using static DTasks.Inspection.InspectionFixtures;

namespace DTasks.Inspection;

public partial class StateMachineInspectorTests
{
    private static Expression<Predicate<FieldInfo>> StateMachineField(string name)
    {
        return field => field.DeclaringType == StateMachineType && field.Name == name;
    }

    private static Expression<Predicate<MethodInfo>> IsSuspendedMethod()
    {
        return method => method.DeclaringType!.Name == nameof(IStateMachineInfo) && method.Name == nameof(IStateMachineInfo.IsSuspended);
    }

    private static Expression<Predicate<MethodInfo>> GetTypeFromHandleMethod()
    {
        return method => method.DeclaringType!.Name == nameof(Type) && method.Name == nameof(Type.GetTypeFromHandle);
    }

    private static Expression<Predicate<MethodInfo>> CreateMethod()
    {
        return method => method.DeclaringType == typeof(AsyncDTaskMethodBuilder<int>) && method.Name == nameof(AsyncDTaskMethodBuilder<int>.Create);
    }

    private static Expression<Predicate<MethodInfo>> StartMethod()
    {
        return method => method.DeclaringType == typeof(AsyncDTaskMethodBuilder<int>) && method.Name == nameof(AsyncDTaskMethodBuilder<int>.Start);
    }

    private static Expression<Predicate<MethodInfo>> TaskGetter()
    {
        return method => method.DeclaringType == typeof(AsyncDTaskMethodBuilder<int>) && method.Name == $"get_{nameof(AsyncDTaskMethodBuilder<int>.Task)}";
    }

    private static Expression<Predicate<MethodInfo>> GetAwaiterMethod()
    {
        return method => method.DeclaringType == typeof(DTask) && method.Name == nameof(DTask.GetAwaiter);
    }

    public delegate void StructSuspender<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo info, CallbackStruct callback)
        where TStateMachine : notnull;

    public delegate void ByRefStructSuspender<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo info, ref CallbackStruct callback)
        where TStateMachine : notnull;

    public delegate void ClassSuspender<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo info, CallbackClass callback)
        where TStateMachine : notnull;

    public delegate void ByRefClassSuspender<TStateMachine>(ref TStateMachine stateMachine, IStateMachineInfo info, ref CallbackClass callback)
        where TStateMachine : notnull;

    public delegate DTask StructResumer(DTask resultTask, CallbackStruct callback);

    public delegate DTask ByRefStructResumer(DTask resultTask, ref CallbackStruct callback);

    public delegate DTask ClassResumer(DTask resultTask, CallbackClass callback);

    public delegate DTask ByRefClassResumer(DTask resultTask, ref CallbackClass callback);

    public struct CallbackStruct { }

    public class CallbackClass { }

    public class Methods
    {
        public static readonly MethodInfo HandleFieldMethod = typeof(Methods).GetMethod(nameof(HandleField))!;
        public static readonly MethodInfo HandleStateMethod = typeof(Methods).GetMethod(nameof(HandleState))!;
        public static readonly MethodInfo HandleAwaiterMethod = typeof(Methods).GetMethod(nameof(HandleAwaiter))!;

        public void HandleField() { }
        public void HandleState() { }
        public void HandleAwaiter() { }
    }
}
