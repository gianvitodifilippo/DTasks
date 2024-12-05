using DTasks.Marshaling;
using DTasks.Utils;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace DTasks.Inspection.Dynamic;

internal readonly ref struct InspectorILGenerator(
    ILGenerator il,
    bool stateMachineIsClass,
    bool loadCallbackByAddress,
    OpCode callMethodOpCode)
{
    private static readonly MethodInfo s_isSuspendedGenericMethod = typeof(ISuspensionContext).GetRequiredMethod(
        name: nameof(ISuspensionContext.IsSuspended),
        genericParameterCount: 1,
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [Type.MakeGenericMethodParameter(0).MakeByRefType()]);

    private static readonly MethodInfo s_getTypeIdMethod = typeof(IAwaiterManager).GetRequiredMethod(
        name: nameof(IAwaiterManager.GetTypeId),
        genericParameterCount: 0,
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: [typeof(object)]);

    public void LoadThis()
    {
        il.Emit(OpCodes.Ldarg_0);
    }

    public void LoadStateMachineArg()
    {
        il.Emit(OpCodes.Ldarg_1);
        if (stateMachineIsClass)
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

    public void LoadField(FieldInfo field)
    {
        il.Emit(OpCodes.Ldfld, field);
    }

    public void LoadFieldAddress(FieldInfo field)
    {
        il.Emit(OpCodes.Ldflda, field);
    }

    public void LoadString(string str)
    {
        il.Emit(OpCodes.Ldstr, str);
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

    public void BranchIfFalse(Label label)
    {
        il.Emit(OpCodes.Brfalse_S, label);
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
            default:
                throw new UnreachableException();
        }
    }

    private void CallCallbackMethod(MethodInfo method)
    {
        il.Emit(callMethodOpCode, method);
    }

    // callback: reader or writer
    public static InspectorILGenerator Create(MethodBuilder method, Type stateMachineType, Type callbackParameterType, Type callbackType)
    {
        return new(
            il: method.GetILGenerator(),
            stateMachineIsClass: !stateMachineType.IsValueType,
            loadCallbackByAddress: callbackType.IsValueType && !callbackParameterType.IsByRef,
            callMethodOpCode: callbackType.IsValueType ? OpCodes.Call : OpCodes.Callvirt);
    }
}
