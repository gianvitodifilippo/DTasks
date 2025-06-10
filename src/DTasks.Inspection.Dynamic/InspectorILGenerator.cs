using System.Reflection;
using System.Reflection.Emit;
using DTasks.Infrastructure.Marshaling;
using DTasks.Infrastructure.State;
using DTasks.Inspection.Dynamic.Descriptors;
using DTasks.Utils;

namespace DTasks.Inspection.Dynamic;

internal readonly ref struct InspectorILGenerator(
    ILGenerator il,
    StateMachineDescriptor stateMachineDescriptor,
    bool loadCallbackByAddress,
    OpCode callMethodOpCode)
{
    private static readonly MethodInfo s_isSuspendedGenericMethod = typeof(IDehydrationContext).GetRequiredMethod(
        name: nameof(IDehydrationContext.IsSuspended),
        genericParameterCount: 1,
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [Type.MakeGenericMethodParameter(0).MakeByRefType()]);

    private static readonly MethodInfo s_getTypeIdMethod = typeof(IAwaiterManager).GetRequiredMethod(
        name: nameof(IAwaiterManager.GetTypeId),
        genericParameterCount: 0,
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [typeof(object)]);

    private static readonly MethodInfo s_completedDTaskGetter = typeof(DTask).GetRequiredMethod(
        name: $"get_{nameof(DTask.CompletedDTask)}",
        genericParameterCount: 0,
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: []);

    private static readonly MethodInfo s_fromResultGenericMethod = typeof(DTask).GetRequiredMethod(
        name: nameof(DTask.FromResult),
        genericParameterCount: 1,
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [Type.MakeGenericMethodParameter(0)]);

    private static readonly MethodInfo s_getAwaiterMethod = typeof(DTask).GetRequiredMethod(
        name: nameof(DTask.GetAwaiter),
        genericParameterCount: 0,
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: []);

    private static readonly ConstructorInfo s_invalidOperationExceptionConstructor = typeof(InvalidOperationException).GetRequiredConstructor(
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [typeof(string)]);

    private static readonly MethodInfo s_createFromVoidMethod = typeof(IAwaiterManager).GetRequiredMethod(
        name: nameof(IAwaiterManager.CreateFromResult),
        genericParameterCount: 0,
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [typeof(TypeId)]);

    private static readonly MethodInfo s_createFromResultGenericMethod = typeof(IAwaiterManager).GetRequiredMethod(
        name: nameof(IAwaiterManager.CreateFromResult),
        genericParameterCount: 1,
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [typeof(TypeId), Type.MakeGenericMethodParameter(0)]);

    private static readonly MethodInfo s_createFromExceptionMethod = typeof(IAwaiterManager).GetRequiredMethod(
        name: nameof(IAwaiterManager.CreateFromException),
        genericParameterCount: 0,
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [typeof(TypeId), typeof(Exception)]);

    private static readonly MethodInfo s_getTypeFromHandleMethod = typeof(Type).GetRequiredMethod(
        name: nameof(Type.GetTypeFromHandle),
        genericParameterCount: 0,
        bindingAttr: BindingFlags.Static | BindingFlags.Public,
        parameterTypes: [typeof(RuntimeTypeHandle)]);

    private static readonly MethodInfo s_typeEqualsMethod = typeof(Type).GetRequiredMethod(
        name: nameof(Type.Equals),
        genericParameterCount: 0,
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [typeof(Type)]);

    public void DeclareStateMachineLocal()
    {
        il.DeclareLocal(stateMachineDescriptor.Type);
    }

    public void DeclareAwaiterIndexLocal()
    {
        il.DeclareLocal(typeof(int));
    }

    public void DeclareAwaiterIdLocal()
    {
        il.DeclareLocal(typeof(TypeId));
    }

    public void InitStateMachine()
    {
        if (stateMachineDescriptor.IsValueType)
        {
            il.Emit(OpCodes.Ldloca_S, 0);
            il.Emit(OpCodes.Initobj, stateMachineDescriptor.Type);
        }
        else
        {
            il.Emit(OpCodes.Newobj, stateMachineDescriptor.Constructor);
            il.Emit(OpCodes.Stloc_0);
        }
    }

    public void InitAwaiterIndex()
    {
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Stloc_1);
    }

    public void InitAwaiterId()
    {
        il.Emit(OpCodes.Ldloca_S, 2);
        il.Emit(OpCodes.Initobj, typeof(TypeId));
    }

    public void LoadThis()
    {
        il.Emit(OpCodes.Ldarg_0);
    }

    public void LoadStateMachineArg()
    {
        il.Emit(OpCodes.Ldarg_1);
        if (!stateMachineDescriptor.IsValueType)
        {
            il.Emit(OpCodes.Ldind_Ref);
        }
    }

    public void LoadSuspensionContext()
    {
        il.Emit(OpCodes.Ldarg_2);
    }

    public void LoadWriter()
    {
        if (loadCallbackByAddress)
        {
            il.Emit(OpCodes.Ldarga_S, 3);
        }
        else
        {
            il.Emit(OpCodes.Ldarg_3);
        }
    }

    public void LoadReader()
    {
        if (loadCallbackByAddress)
        {
            il.Emit(OpCodes.Ldarga_S, 1);
        }
        else
        {
            il.Emit(OpCodes.Ldarg_1);
        }
    }

    public void LoadResult()
    {
        il.Emit(OpCodes.Ldarg_2);
    }

    public void LoadException()
    {
        il.Emit(OpCodes.Ldarg_2);
    }

    public void LoadStateMachineLocal()
    {
        if (stateMachineDescriptor.IsValueType)
        {
            il.Emit(OpCodes.Ldloca_S, 0);
        }
        else
        {
            il.Emit(OpCodes.Ldloc_0);
        }
    }

    public void LoadStateMachineLocalAddress()
    {
        il.Emit(OpCodes.Ldloca_S, 0);
    }

    public void LoadAwaiterIndex()
    {
        il.Emit(OpCodes.Ldloc_1);
    }

    public void LoadAwaiterIndexAddress()
    {
        il.Emit(OpCodes.Ldloca_S, 1);
    }

    public void LoadAwaiterId()
    {
        il.Emit(OpCodes.Ldloc_2);
    }

    public void LoadAwaiterIdAddress()
    {
        il.Emit(OpCodes.Ldloca_S, 2);
    }

    public void Call(MethodInfo method)
    {
        OpCode opCode = method.IsStatic || method.DeclaringType!.IsValueType
            ? OpCodes.Call
            : OpCodes.Callvirt;

        il.Emit(opCode, method);
    }

    public void CallWriterMethod(MethodInfo method)
    {
        CallCallbackMethod(method);
    }

    public void CallReaderMethod(MethodInfo method)
    {
        CallCallbackMethod(method);
    }

    public void CallIsSuspendedMethod(Type awaiterType)
    {
        MethodInfo isSuspendedMethod = s_isSuspendedGenericMethod.MakeGenericMethod(awaiterType);

        il.Emit(OpCodes.Callvirt, isSuspendedMethod);
    }

    public void CallGetTypeIdMethod()
    {
        il.Emit(OpCodes.Callvirt, s_getTypeIdMethod);
    }

    public void CallCompletedDTaskGetter()
    {
        il.Emit(OpCodes.Call, s_completedDTaskGetter);
    }

    public void CallGetAwaiterMethod()
    {
        // Relies on DTask.Awaiter and DTask<TResult>.Awaiter having the same layout
        il.Emit(OpCodes.Callvirt, s_getAwaiterMethod);
    }

    public void CallCreateFromVoidMethod()
    {
        il.Emit(OpCodes.Callvirt, s_createFromVoidMethod);
    }

    public void CallCreateFromResultMethod(Type resultType)
    {
        MethodInfo createFromResultMethod = s_createFromResultGenericMethod.MakeGenericMethod(resultType);

        il.Emit(OpCodes.Callvirt, createFromResultMethod);
    }

    public void CallCreateFromExceptionMethod()
    {
        il.Emit(OpCodes.Callvirt, s_createFromExceptionMethod);
    }

    public void CallBuilderCreateMethod()
    {
        il.Emit(OpCodes.Call, stateMachineDescriptor.BuilderCreateMethod);
    }

    public void CallBuilderStartMethod()
    {
        OpCode opCode = stateMachineDescriptor.BuilderField.FieldType.IsValueType
            ? OpCodes.Call
            : OpCodes.Callvirt;

        il.Emit(opCode, stateMachineDescriptor.BuilderStartMethod.MakeGenericMethod(stateMachineDescriptor.Type));
    }

    public void CallBuilderTaskGetter()
    {
        OpCode opCode = stateMachineDescriptor.BuilderField.FieldType.IsValueType
            ? OpCodes.Call
            : OpCodes.Callvirt;

        il.Emit(opCode, stateMachineDescriptor.BuilderTaskGetter);
    }

    public void CallFromResultMethod(Type resultType)
    {
        MethodInfo fromResultMethod = s_fromResultGenericMethod.MakeGenericMethod(resultType);

        il.Emit(OpCodes.Call, fromResultMethod);
    }

    public void CallFromExceptionMethod()
    {
        MethodInfo fromExceptionMethod = typeof(DTask).GetRequiredMethod(
            name: nameof(DTask.FromException),
            genericParameterCount: 0,
            bindingAttr: BindingFlags.Static | BindingFlags.Public,
            parameterTypes: [typeof(Exception)]);
        
        il.Emit(OpCodes.Call, fromExceptionMethod);
    }

    public void CallFromExceptionMethod(Type resultType)
    {
        Type taskType = typeof(DTask<>).MakeGenericType(resultType);
        MethodInfo fromExceptionMethod = taskType.GetRequiredMethod(
            name: nameof(DTask.FromException),
            genericParameterCount: 0,
            bindingAttr: BindingFlags.Static | BindingFlags.Public,
            parameterTypes: [typeof(Exception)]);
        
        il.Emit(OpCodes.Call, fromExceptionMethod);
    }

    public void CallGetTypeFromHandleMethod()
    {
        il.Emit(OpCodes.Call, s_getTypeFromHandleMethod);
    }

    public void CallTypeEqualsMethod()
    {
        il.Emit(OpCodes.Call, s_typeEqualsMethod);
    }

    public void NewInvalidOperationException()
    {
        il.Emit(OpCodes.Newobj, s_invalidOperationExceptionConstructor);
    }

    public void LoadField(FieldInfo field)
    {
        il.Emit(OpCodes.Ldfld, field);
    }

    public void LoadFieldAddress(FieldInfo field)
    {
        il.Emit(OpCodes.Ldflda, field);
    }

    public void StoreField(FieldInfo field)
    {
        il.Emit(OpCodes.Stfld, field);
    }

    public void LoadString(string str)
    {
        il.Emit(OpCodes.Ldstr, str);
    }

    public void LoadToken(Type type)
    {
        il.Emit(OpCodes.Ldtoken, type);
    }

    public Label DefineLabel()
    {
        return il.DefineLabel();
    }

    public void MarkLabel(Label label)
    {
        il.MarkLabel(label);
    }

    public void Return()
    {
        il.Emit(OpCodes.Ret);
    }

    public void Throw()
    {
        il.Emit(OpCodes.Throw);
    }

    public void BranchIfFalse(Label label, bool shortForm = false)
    {
        OpCode opCode = shortForm ? OpCodes.Brfalse_S : OpCodes.Brfalse;
        il.Emit(opCode, label);
    }

    public void BranchIfTrue(Label label, bool shortForm = false)
    {
        OpCode opCode = shortForm ? OpCodes.Brtrue_S : OpCodes.Brtrue;
        il.Emit(opCode, label);
    }

    public void Switch(Label[] labels)
    {
        il.Emit(OpCodes.Switch, labels);
    }

    public void Branch(Label label, bool shortForm = false)
    {
        OpCode opCode = shortForm ? OpCodes.Br_S : OpCodes.Br;
        il.Emit(opCode, label);
    }

    public void Pop()
    {
        il.Emit(OpCodes.Pop);
    }

    public void Subtract()
    {
        il.Emit(OpCodes.Sub);
    }

    public void LoadInt(int value)
    {
        switch (value)
        {
            case 0:
                il.Emit(OpCodes.Ldc_I4_0);
                break;
            case 1:
                il.Emit(OpCodes.Ldc_I4_1);
                break;
            case 2:
                il.Emit(OpCodes.Ldc_I4_2);
                break;
            case 3:
                il.Emit(OpCodes.Ldc_I4_3);
                break;
            case 4:
                il.Emit(OpCodes.Ldc_I4_4);
                break;
            case 5:
                il.Emit(OpCodes.Ldc_I4_5);
                break;
            case 6:
                il.Emit(OpCodes.Ldc_I4_6);
                break;
            case 7:
                il.Emit(OpCodes.Ldc_I4_7);
                break;
            case 8:
                il.Emit(OpCodes.Ldc_I4_8);
                break;
            case -1:
                il.Emit(OpCodes.Ldc_I4_M1);
                break;
            case >= -128 and < -1 or > 8 and < 128:
                il.Emit(OpCodes.Ldc_I4_S, value);
                break;
            case < -128 or >= 128:
                il.Emit(OpCodes.Ldc_I4, value);
                break;
        }
    }

    private void CallCallbackMethod(MethodInfo method)
    {
        il.Emit(callMethodOpCode, method);
    }

    // callback: reader or writer
    public static InspectorILGenerator Create(MethodBuilder method, StateMachineDescriptor stateMachineDescriptor, Type callbackParameterType, Type callbackType)
    {
        return new(
            il: method.GetILGenerator(),
            stateMachineDescriptor: stateMachineDescriptor,
            loadCallbackByAddress: callbackType.IsValueType && !callbackParameterType.IsByRef,
            callMethodOpCode: callbackType.IsValueType ? OpCodes.Call : OpCodes.Callvirt);
    }
}
