using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using DTasks.Configuration.DependencyInjection;
using DTasks.Infrastructure;
using DTasks.Infrastructure.Marshaling;
using DTasks.Inspection;
using DTasks.Utils;

namespace DTasks.Configuration;

public interface IMarshalingConfigurationBuilder
{
    IMarshalingConfigurationBuilder AddSurrogator(IComponentDescriptor<IDAsyncSurrogator> descriptor);

    IMarshalingConfigurationBuilder RegisterSurrogatableType(Type type);

    IMarshalingConfigurationBuilder RegisterTypeId(Type type);
}

public static class MarshalingConfigurationBuilderExtensions // TODO: Convert to generic methods returning TBuilder
{
    public static IMarshalingConfigurationBuilder RegisterTypeIds(this IMarshalingConfigurationBuilder builder, IEnumerable<Type> types)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(types);

        foreach (Type type in types)
        {
            builder.RegisterTypeId(type);
        }

        return builder;
    }

    public static IMarshalingConfigurationBuilder RegisterDAsyncMethod(this IMarshalingConfigurationBuilder builder, MethodInfo method)
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

        return builder.RegisterTypeId(stateMachineType);
    }

    public static IMarshalingConfigurationBuilder RegisterDAsyncType(this IMarshalingConfigurationBuilder builder, Type type)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(type);

        return RegisterDAsyncTypeCore(builder, type);
    }

    public static IMarshalingConfigurationBuilder RegisterDAsyncType<T>(this IMarshalingConfigurationBuilder builder)
    {
        ThrowHelper.ThrowIfNull(builder);

        return RegisterDAsyncTypeCore(builder, typeof(T));
    }

    private static IMarshalingConfigurationBuilder RegisterDAsyncTypeCore(IMarshalingConfigurationBuilder builder, Type type)
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
            builder.RegisterTypeId(stateMachineType);

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

        return builder;
    }
}
