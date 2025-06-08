using System.Linq.Expressions;
using System.Reflection;
using DTasks.CompilerServices;
using DTasks.Infrastructure.Marshaling;
using static DTasks.Inspection.Dynamic.InspectionFixtures;

namespace DTasks.Inspection.Dynamic;

public partial class DynamicStateMachineInspectorTests
{
    private static Expression<Predicate<FieldInfo>> StateMachineField(string name)
    {
        return field => field.DeclaringType == StateMachineType && field.Name == name;
    }

    private static Expression<Predicate<FieldInfo>> SuspenderField(string name)
    {
        return field =>
            field.DeclaringType != null &&
            field.DeclaringType.Name.EndsWith("Suspender") &&
            field.Name == name;
    }

    private static Expression<Predicate<FieldInfo>> ResumerField(string name)
    {
        return field =>
            field.DeclaringType != null &&
            field.DeclaringType.Name.EndsWith("Resumer") &&
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
            method.GetGenericArguments()[0].IsGenericParameter &&
            method.DeclaringType == typeof(IAwaiterManager);
    }

    private static Expression<Predicate<MethodInfo>> CreateFromExceptionMethod()
    {
        return method =>
            method.Name == nameof(IAwaiterManager.CreateFromException) &&
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

    private static Expression<Predicate<MethodInfo>> DTaskFromResultMethod()
    {
        return method =>
            method.Name == nameof(DTask.FromResult) &&
            method.GetGenericArguments()[0].IsGenericParameter &&
            method.DeclaringType == typeof(DTask);
    }

    private static Expression<Predicate<MethodInfo>> DTaskFromExceptionMethod(Type resultType)
    {
        return method =>
            method.Name == nameof(DTask.FromException) &&
            method.DeclaringType == typeof(DTask<>).MakeGenericType(resultType);
    }

    private static Expression<Predicate<MethodInfo>> GetTypeFromHandleMethod()
    {
        return method =>
            method.Name == nameof(Type.GetTypeFromHandle) &&
            method.DeclaringType == typeof(Type);
    }

    private static Expression<Predicate<MethodInfo>> TypeEqualsMethod()
    {
        return method =>
            method.Name == nameof(Type.Equals) &&
            method.GetParameters()[0].ParameterType == typeof(Type) &&
            method.DeclaringType == typeof(Type);
    }

    private static Expression<Predicate<ConstructorInfo>> ObjectConstructor()
    {
        return constructor => constructor.DeclaringType == typeof(object);
    }

    private static Expression<Predicate<ConstructorInfo>> InvalidOperationExceptionConstructor()
    {
        return constructor => constructor.DeclaringType == typeof(InvalidOperationException);
    }

    private static Expression<Predicate<Type>> GenericMethodParameter(int position)
    {
        return type => type.IsGenericMethodParameter && type.GenericParameterPosition == position;
    }

    private static Expression<Predicate<MethodInfo>> GetAwaiterMethod()
    {
        return method =>
            method.Name == nameof(DTask.GetAwaiter) &&
            method.DeclaringType == typeof(DTask);
    }
}
