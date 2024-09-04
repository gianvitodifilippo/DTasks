using DTasks.CompilerServices;
using DTasks.Hosting;
using System.Reflection;
using System.Reflection.Emit;

namespace DTasks.Inspection;

internal readonly ref struct InspectorILGenerator(
    ILGenerator il,
    StateMachineDescriptor stateMachineDescriptor,
    bool loadCallbackByAddress,
    bool loadCallbackIndirectly,
    OpCode callOpCode)
{
    private static readonly MethodInfo _isSuspendedGenericMethod = typeof(InspectorILGenerator).GetMethod(
        name: nameof(IsSuspended),
        bindingAttr: BindingFlags.Static | BindingFlags.NonPublic)!;

    private static readonly MethodInfo _getAwaiterMethod = typeof(DTask).GetMethod(
        name: nameof(DTask.GetAwaiter),
        bindingAttr: BindingFlags.Instance | BindingFlags.Public)!;

    private Type StateMachineType => stateMachineDescriptor.Type;

    public void LoadDeconstructor()
    {
        if (loadCallbackByAddress)
        {
            il.Emit(OpCodes.Ldarga_S, 2);
        }
        else
        {
            il.Emit(OpCodes.Ldarg_2);
        }

        if (loadCallbackIndirectly)
        {
            il.Emit(OpCodes.Ldind_Ref);
        }
    }

    public void LoadConstructor()
    {
        if (loadCallbackByAddress)
        {
            il.Emit(OpCodes.Ldarga_S, 1);
        }
        else
        {
            il.Emit(OpCodes.Ldarg_1);
        }

        if (loadCallbackIndirectly)
        {
            il.Emit(OpCodes.Ldind_Ref);
        }
    }

    public void LoadStateMachineArg()
    {
        il.Emit(OpCodes.Ldarg_0);
        if (!StateMachineType.IsValueType)
        {
            il.Emit(OpCodes.Ldind_Ref);
        }
    }

    public void CreateStateMachine()
    {
        if (StateMachineType.IsValueType)
        {
            il.Emit(OpCodes.Ldloca_S, 0);
            il.Emit(OpCodes.Initobj, StateMachineType);
        }
        else
        {
            il.Emit(OpCodes.Newobj, stateMachineDescriptor.Constructor);
            il.Emit(OpCodes.Stloc_0);
        }
    }

    public void LoadStateMachineLocal()
    {
        if (StateMachineType.IsValueType)
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

    public void LoadStateMachineInfo()
    {
        il.Emit(OpCodes.Ldarg_1);
    }

    public void LoadResultTask()
    {
        il.Emit(OpCodes.Ldarg_0);
    }

    public void Return()
    {
        il.Emit(OpCodes.Ret);
    }

    public Label DefineLabel()
    {
        return il.DefineLabel();
    }

    public void BranchIfFalse(Label label)
    {
        il.Emit(OpCodes.Brfalse_S, label);
    }

    public void MarkLabel(Label label)
    {
        il.MarkLabel(label);
    }

    public void CallHandleMethod(MethodInfo method)
    {
        il.Emit(callOpCode, method);
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

    public void CallIsSuspendedMethod(Type awaiterType)
    {
        MethodInfo isSuspendedMethod = _isSuspendedGenericMethod.MakeGenericMethod(awaiterType);
        il.Emit(OpCodes.Call, isSuspendedMethod);
    }

    public void CreateAsyncMethodBuilder(Type builderType)
    {
        MethodInfo createMethod = builderType.GetMethod(nameof(AsyncDTaskMethodBuilder.Create))!;
        il.Emit(OpCodes.Call, createMethod);
    }

    public void CallStartMethod(Type builderType)
    {
        MethodInfo startMethod = builderType.GetMethod(nameof(AsyncDTaskMethodBuilder.Start))!.MakeGenericMethod(StateMachineType);
        il.Emit(OpCodes.Call, startMethod);
    }

    public void CallTaskGetter(Type builderType)
    {
        MethodInfo taskGetter = builderType.GetMethod($"get_{nameof(AsyncDTaskMethodBuilder.Task)}")!;
        il.Emit(OpCodes.Call, taskGetter);
    }

    public void CallGetAwaiterMethod()
    {
        il.Emit(OpCodes.Callvirt, _getAwaiterMethod);
    }

    public void LoadString(string str)
    {
        il.Emit(OpCodes.Ldstr, str);
    }

    public void Pop()
    {
        il.Emit(OpCodes.Pop);
    }

    public void DeclareStateMachineLocal()
    {
        il.DeclareLocal(StateMachineType);
    }

    public static InspectorILGenerator Create(DynamicMethod method, StateMachineDescriptor stateMachineDescriptor, Type callbackType, Type callbackParameterType)
    {
        // callback: constructor or deconstructor
        return new(
            method.GetILGenerator(),
            stateMachineDescriptor: stateMachineDescriptor,
            loadCallbackByAddress: callbackType.IsValueType && !callbackParameterType.IsByRef,
            loadCallbackIndirectly: !callbackType.IsValueType && callbackParameterType.IsByRef,
            callOpCode: !callbackType.IsValueType ? OpCodes.Callvirt : OpCodes.Call);
    }

    private static bool IsSuspended<TAwaiter>(IStateMachineInfo info) => info.SuspendedAwaiterType == typeof(TAwaiter);
}
