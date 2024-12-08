using DTasks.CompilerServices;
using DTasks.Marshaling;
using System.Linq.Expressions;
using System.Reflection;
using static DTasks.Inspection.Dynamic.InspectionFixtures;

namespace DTasks.Inspection.Dynamic;

public partial class DynamicStateMachineInspectorTests
{
    private static Expression<Predicate<FieldInfo>> StateMachineField(string name)
    {
        return field => field.DeclaringType == StateMachineType && field.Name == name;
    }

    private static Expression<Predicate<FieldInfo>> ConverterField(string name)
    {
        return field =>
            field.DeclaringType != null &&
            field.DeclaringType.Name.EndsWith("Converter") &&
            field.Name == name;
    }

    private static Expression<Predicate<MethodInfo>> IsSuspendedMethod(Type awaiterType)
    {
        return method =>
            method.Name == nameof(ISuspensionContext.IsSuspended) &&
            method.IsConstructedGenericMethod &&
            method.GetGenericArguments().Length == 1 &&
            method.GetGenericArguments()[0] == awaiterType &&
            method.DeclaringType == typeof(ISuspensionContext);
    }

    private static Expression<Predicate<MethodInfo>> GetTypeIdMethod()
    {
        return method =>
            method.Name == nameof(IAwaiterManager.GetTypeId) &&
            method.DeclaringType == typeof(IAwaiterManager);
    }

    private static Expression<Predicate<MethodInfo>> CreateFromVoidMethod()
    {
        return method =>
            method.Name == nameof(IAwaiterManager.CreateFromResult) &&
            !method.IsGenericMethod &&
            method.DeclaringType == typeof(IAwaiterManager);
    }

    private static Expression<Predicate<MethodInfo>> CreateFromResultMethod()
    {
        return method =>
            method.Name == nameof(IAwaiterManager.CreateFromResult) &&
            method.IsGenericMethod &&
            method.DeclaringType == typeof(IAwaiterManager);
    }

    private static Expression<Predicate<MethodInfo>> BuilderCreateMethod()
    {
        return method =>
            method.Name == nameof(AsyncDTaskMethodBuilder<int>.Create) &&
            method.DeclaringType == typeof(AsyncDTaskMethodBuilder<int>);
    }

    private static Expression<Predicate<MethodInfo>> BuilderStartMethod()
    {
        return method =>
            method.Name == nameof(AsyncDTaskMethodBuilder<int>.Start) &&
            method.DeclaringType == typeof(AsyncDTaskMethodBuilder<int>);
    }

    private static Expression<Predicate<MethodInfo>> BuilderTaskGetter()
    {
        return method =>
            method.Name == $"get_{nameof(AsyncDTaskMethodBuilder<int>.Task)}" &&
            method.DeclaringType == typeof(AsyncDTaskMethodBuilder<int>);
    }

    private static Expression<Predicate<ConstructorInfo>> ObjectConstructor()
    {
        return constructor => constructor.DeclaringType == typeof(object);
    }

    private static Expression<Predicate<ConstructorInfo>> InvalidOperationExceptionConstructor()
    {
        return constructor => constructor.DeclaringType == typeof(InvalidOperationException);
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
}
