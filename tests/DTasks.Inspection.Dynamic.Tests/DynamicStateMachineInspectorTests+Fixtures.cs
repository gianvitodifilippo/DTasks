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

    private static Expression<Predicate<MethodInfo>> GetTypeMethod()
    {
        return method => method.Name == nameof(GetType) && method.DeclaringType == typeof(object);
    }

    private static Expression<Predicate<MethodInfo>> GetTypeIdMethod()
    {
        return method => method.Name == nameof(ITypeResolver.GetTypeId) && method.DeclaringType == typeof(ITypeResolver);
    }

    private static Expression<Predicate<MethodInfo>> TypeIdValueGetter()
    {
        return method => method.Name == $"get_{nameof(TypeId.Value)}" && method.DeclaringType == typeof(TypeId);
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
