using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using DTasks.Inspection;
using DTasks.Utils;

namespace DTasks.Infrastructure.Marshaling;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class TypeResolverBuilderExtensions
{
    public static void RegisterDAsyncMethod(this DAsyncTypeResolverBuilder builder, MethodInfo method)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(method);

        if (method.ContainsGenericParameters)
            throw new ArgumentException("Open generic methods are not supported.", nameof(method));

        AsyncStateMachineAttribute? attribute = method.GetCustomAttribute<AsyncStateMachineAttribute>();
        if (attribute is null)
            throw new ArgumentException($"Method '{method.Name}' of type '{method.DeclaringType!.Name}' is not async.", nameof(method));

        Type stateMachineType = attribute.StateMachineType;

        if (method.IsGenericMethod)
        {
            Type[] genericTypeArguments = method.GetGenericArguments();
            stateMachineType = stateMachineType.MakeGenericType(genericTypeArguments);
        }

        builder.Register(stateMachineType);
    }

    public static void RegisterDAsyncType(this DAsyncTypeResolverBuilder builder, Type type)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(type);

        RegisterDAsyncTypeCore(builder, type);
    }

    public static void RegisterDAsyncType<T>(this DAsyncTypeResolverBuilder builder)
    {
        ThrowHelper.ThrowIfNull(builder);

        RegisterDAsyncTypeCore(builder, typeof(T));
    }

    private static void RegisterDAsyncTypeCore(DAsyncTypeResolverBuilder builder, Type type)
    {
        if (type.ContainsGenericParameters)
            throw new ArgumentException("Invalid d-async type.", nameof(type));

        MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (MethodInfo method in methods)
        {
            if (method.IsGenericMethodDefinition)
                continue;

            if (!typeof(DTask).IsAssignableFrom(method.ReturnType))
                continue;

            AsyncStateMachineAttribute? asyncStateMachineAttribute = method.GetCustomAttribute<AsyncStateMachineAttribute>();
            if (asyncStateMachineAttribute is null)
                continue;

            Type stateMachineType = asyncStateMachineAttribute.StateMachineType;
            builder.Register(stateMachineType);

            // TODO: This is probably not the right place to do this
            FieldInfo[] stateMachineFields = stateMachineType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (FieldInfo stateMachineField in stateMachineFields)
            {
                if (StateMachineFacts.GetFieldKind(stateMachineField) != StateMachineFieldKind.DAsyncAwaiterField)
                    continue;

                Type fieldType = stateMachineField.FieldType;
                if (!fieldType.IsGenericType || fieldType.GetGenericTypeDefinition() != typeof(DTask<>.Awaiter))
                    continue;

                Type[] genericTypeArguments = fieldType.GetGenericArguments();
                Debug.Assert(genericTypeArguments.Length == 1);

                DAsyncFlow.RegisterGenericTypeIds(builder, genericTypeArguments[0]);
            }
        }
    }
}
