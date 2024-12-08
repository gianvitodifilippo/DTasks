using DTasks.Marshaling;
using DTasks.Utils;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;

namespace DTasks.Inspection.Dynamic;

internal class AwaiterManager(DynamicAssembly assembly, ITypeResolver typeResolver) : IAwaiterManager
{
    private static readonly MethodInfo s_fromVoidMethod = typeof(AwaiterFactory).GetRequiredMethod(
        name: nameof(AwaiterFactory.FromResult),
        genericParameterCount: 0,
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: []);

    private static readonly MethodInfo s_fromResultMethod = typeof(AwaiterFactory).GetRequiredMethod(
        name: nameof(AwaiterFactory.FromResult),
        genericParameterCount: 1,
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [Type.MakeGenericMethodParameter(0)]);

    private static readonly MethodInfo s_fromExceptionMethod = typeof(AwaiterFactory).GetRequiredMethod(
        name: nameof(AwaiterFactory.FromException),
        genericParameterCount: 0,
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [typeof(Exception)]);

    private readonly ConcurrentDictionary<TypeId, AwaiterFactory> _factories = [];

    public TypeId GetTypeId(object awaiter)
    {
        return typeResolver.GetTypeId(awaiter.GetType());
    }

    public object CreateFromResult(TypeId awaiterId)
    {
        return GetFactory(awaiterId).FromResult();
    }

    public object CreateFromResult<TResult>(TypeId awaiterId, TResult result)
    {
        return GetFactory(awaiterId).FromResult(result);
    }

    public object CreateFromException(TypeId awaiterId, Exception exception)
    {
        return GetFactory(awaiterId).FromException(exception);
    }

    private AwaiterFactory GetFactory(TypeId awaiterId)
    {
        return _factories.GetOrAdd(awaiterId, CreateFactory, this);

        static AwaiterFactory CreateFactory(TypeId awaiterId, AwaiterManager self)
            => self.CreateFactory(awaiterId);
    }

    private AwaiterFactory CreateFactory(TypeId awaiterId)
    {
        Type awaiterType = typeResolver.GetType(awaiterId);
        TypeBuilder factoryType = assembly.DefineAwaiterFactoryType(awaiterType);
        factoryType.SetParent(typeof(AwaiterFactory));

        TryOverrideFromVoidMethod(factoryType, awaiterType);
        TryOverrideFromResultMethod(factoryType, awaiterType);
        TryOverrideFromExceptionMethod(factoryType, awaiterType);

        return (AwaiterFactory)Activator.CreateInstance(factoryType.CreateType())!;
    }

    private static void TryOverrideFromVoidMethod(TypeBuilder factoryType, Type awaiterType)
    {
        MethodInfo? awaiterFromVoidMethod = awaiterType.GetMethod(
            name: "FromResult",
            genericParameterCount: 0,
            bindingAttr: BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: [],
            modifiers: null);

        if (awaiterFromVoidMethod is null)
            return;

        MethodBuilder fromVoidMethod = factoryType.DefineMethodOverride(s_fromVoidMethod);
        ILGenerator il = fromVoidMethod.GetILGenerator();
        
        il.Emit(OpCodes.Call, awaiterFromVoidMethod);
        il.Emit(OpCodes.Ret);
    }

    private static void TryOverrideFromResultMethod(TypeBuilder factoryType, Type awaiterType)
    {
        MethodInfo? awaiterFromResultGenericMethod = awaiterType.GetMethod(
            name: "FromResult",
            genericParameterCount: 1,
            bindingAttr: BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: [Type.MakeGenericMethodParameter(0)],
            modifiers: null);

        if (awaiterFromResultGenericMethod is null)
            return;

        MethodBuilder fromResultMethod = factoryType.DefineMethodOverride(s_fromResultMethod);
        MethodInfo awaiterFromResultMethod = awaiterFromResultGenericMethod.MakeGenericMethod(fromResultMethod.GetGenericArguments());
        ILGenerator il = fromResultMethod.GetILGenerator();

        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Call, awaiterFromResultMethod);
        il.Emit(OpCodes.Ret);
    }

    private static void TryOverrideFromExceptionMethod(TypeBuilder factoryType, Type awaiterType)
    {
        MethodInfo? awaiterFromExceptionMethod = awaiterType.GetMethod(
            name: "FromException",
            genericParameterCount: 0,
            bindingAttr: BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            types: [typeof(Exception)],
            modifiers: null);

        if (awaiterFromExceptionMethod is null)
            return;

        MethodBuilder fromExceptionMethod = factoryType.DefineMethodOverride(s_fromExceptionMethod);
        ILGenerator il = fromExceptionMethod.GetILGenerator();

        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Call, awaiterFromExceptionMethod);
        il.Emit(OpCodes.Ret);
    }

    internal class AwaiterFactory
    {
        public virtual object FromResult() => throw new InvalidOperationException("Invalid attempt to resume a d-async method.");

        public virtual object FromResult<TResult>(TResult result) => throw new InvalidOperationException("Invalid attempt to resume a d-async method.");

        public virtual object FromException(Exception exception) => throw new InvalidOperationException("Invalid attempt to resume a d-async method.");
    }
}
