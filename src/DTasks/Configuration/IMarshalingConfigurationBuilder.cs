using System.Reflection;
using System.Runtime.CompilerServices;
using DTasks.Configuration.DependencyInjection;
using DTasks.Infrastructure.Generics;
using DTasks.Infrastructure.Marshaling;
using DTasks.Metadata;
using DTasks.Utils;

namespace DTasks.Configuration;

public interface IMarshalingConfigurationBuilder
{
    IMarshalingConfigurationBuilder AddSurrogator(IComponentDescriptor<IDAsyncSurrogator> descriptor);

    IMarshalingConfigurationBuilder RegisterSurrogatableType(ITypeContext typeContext);

    IMarshalingConfigurationBuilder RegisterTypeId(Type type, TypeEncodingStrategy encodingStrategy = TypeEncodingStrategy.FullName);

    IMarshalingConfigurationBuilder RegisterTypeId(Type type, string idValue);
}

public static class MarshalingConfigurationBuilderExtensions // TODO: Convert to generic methods returning TBuilder
{
    public static IMarshalingConfigurationBuilder RegisterSurrogatableType<TSurrogatable>(this IMarshalingConfigurationBuilder builder)
    {
        ThrowHelper.ThrowIfNull(builder);
        
        return builder.RegisterSurrogatableType(TypeContext.Of<TSurrogatable>());
    }
    
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

    public static IMarshalingConfigurationBuilder RegisterDAsyncMethods(this IMarshalingConfigurationBuilder builder, Assembly assembly)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(assembly);

        foreach (Type type in assembly.GetTypes())
        {
            if (type.ContainsGenericParameters)
                continue;
            
            RegisterDAsyncTypeCore(builder, type);
        }

        return builder;
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

            if (!method.ReturnType.IsDefined(typeof(DAsyncAttribute)))
                continue;

            AsyncStateMachineAttribute? asyncStateMachineAttribute = method.GetCustomAttribute<AsyncStateMachineAttribute>();
            if (asyncStateMachineAttribute is null)
                continue;

            Type stateMachineType = asyncStateMachineAttribute.StateMachineType;
            builder.RegisterTypeId(stateMachineType);
        }

        return builder;
    }
}
