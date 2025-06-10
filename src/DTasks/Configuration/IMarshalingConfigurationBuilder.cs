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

    IMarshalingConfigurationBuilder RegisterTypeId(ITypeContext typeContext, TypeId typeId);
}

public static class MarshalingConfigurationBuilderExtensions // TODO: Convert to generic methods returning TBuilder
{
    private static readonly MethodInfo s_typeContextStateMachineGenericMethod = typeof(TypeContext).GetRequiredMethod(
        name: nameof(TypeContext.StateMachine),
        genericParameterCount: 1,
        bindingAttr: BindingFlags.Static | BindingFlags.NonPublic,
        parameterTypes: []);
    
    public static IMarshalingConfigurationBuilder RegisterSurrogatableType<TSurrogatable>(this IMarshalingConfigurationBuilder builder)
    {
        ThrowHelper.ThrowIfNull(builder);
        
        return builder.RegisterSurrogatableType(TypeContext.Of<TSurrogatable>());
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
        
        return builder.RegisterStateMachineTypeId(stateMachineType);
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

            builder.RegisterStateMachineTypeId(asyncStateMachineAttribute.StateMachineType);
        }

        return builder;
    }

    private static IMarshalingConfigurationBuilder RegisterStateMachineTypeId(this IMarshalingConfigurationBuilder builder, Type stateMachineType)
    {
        MethodInfo typeContextStateMachineMethod = s_typeContextStateMachineGenericMethod.MakeGenericMethod(stateMachineType);
        ITypeContext typeContext = (ITypeContext)typeContextStateMachineMethod.Invoke(null, null)!;

        TypeId typeId = TypeId.FromEncodedTypeName(stateMachineType);
        return builder.RegisterTypeId(typeContext, typeId);
    }
}
