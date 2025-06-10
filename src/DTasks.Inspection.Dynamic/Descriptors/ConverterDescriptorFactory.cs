using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DTasks.Infrastructure;
using DTasks.Infrastructure.State;
using DTasks.Utils;

namespace DTasks.Inspection.Dynamic.Descriptors;

internal sealed class ConverterDescriptorFactory(
    Type suspenderGenericType,
    Type writerParameterType,
    IWriterDescriptor writer,
    IResumerDescriptor resumerDescriptor) : IConverterDescriptorFactory
{
    public IResumerDescriptor ResumerDescriptor => resumerDescriptor;

    public ISuspenderDescriptor CreateSuspenderDescriptor(Type stateMachineType)
    {
        Type suspenderType = suspenderGenericType.MakeGenericType(stateMachineType);
        MethodInfo suspendMethod = suspenderType.GetRequiredMethod(
            name: InspectionConstants.SuspendMethodName,
            genericParameterCount: 0,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [stateMachineType.MakeByRefType(), typeof(IDehydrationContext), writerParameterType]);

        return new SuspenderDescriptor(suspenderType, suspendMethod, writer);
    }

    public static bool TryCreate(Type suspenderType, Type resumerType, [NotNullWhen(true)] out ConverterDescriptorFactory? factory)
    {
        if (!suspenderType.IsInterface || !resumerType.IsInterface)
            return False(out factory);

        if (!suspenderType.IsGenericTypeDefinition || resumerType.IsGenericTypeDefinition)
            return False(out factory);

        if (suspenderType.GetGenericArguments() is not [Type stateMachineType])
            return False(out factory);

        if (stateMachineType.BaseType != typeof(object))
            return False(out factory);

        if (stateMachineType.GetInterfaces() is not [])
            return False(out factory);

        if (stateMachineType.GetGenericParameterConstraints() is not [])
            return False(out factory);

        MethodInfo[] suspenderMethods = suspenderType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        MethodInfo[] resumerMethods = resumerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        bool hasSuspendMethod = false;
        MethodInfo? resumeWithVoidMethod = null;
        MethodInfo? resumeWithResultMethod = null;
        MethodInfo? resumeWithExceptionMethod = null;
        Type? writerParameterType = null;
        Type? readerParameterType = null;

        foreach (MethodInfo method in suspenderMethods)
        {
            if (method.Name != InspectionConstants.SuspendMethodName)
                return False(out factory);

            if (!HasSuspendMethod(method))
                return False(out factory);
        }

        foreach (MethodInfo method in resumerMethods)
        {
            if (method.Name != InspectionConstants.ResumeMethodName)
                return False(out factory);

            ParameterInfo[] parameters = method.GetParameters();
            switch (parameters.Length)
            {
                case 1:
                    if (!HasResumeWithVoidMethod(method, parameters))
                        return False(out factory);
                    break;

                case 2:
                    if (!HasResumeWithResultMethod(method, parameters) && !HasResumeWithExceptionMethod(method, parameters))
                        return False(out factory);
                    break;

                default:
                    return False(out factory);
            }
        }

        if (!hasSuspendMethod || resumeWithVoidMethod is null || resumeWithResultMethod is null || resumeWithExceptionMethod is null)
            return False(out factory);

        if (readerParameterType is null || writerParameterType is null)
            return False(out factory);

        Type readerType = readerParameterType.IsByRef
            ? readerParameterType.GetElementType()!
            : readerParameterType;

        if (!readerType.IsValueType && readerParameterType.IsByRef)
            return False(out factory);

        Type writerType = writerParameterType.IsByRef
            ? writerParameterType.GetElementType()!
            : writerParameterType;

        if (!writerType.IsValueType && writerParameterType.IsByRef)
            return False(out factory);

        if (!ReaderDescriptor.TryCreate(readerType, out ReaderDescriptor? reader))
            return False(out factory);

        if (!WriterDescriptor.TryCreate(writerType, out WriterDescriptor? writer))
            return False(out factory);

        ResumerDescriptor resumerDescriptor = new(resumerType, resumeWithVoidMethod, resumeWithResultMethod, resumeWithExceptionMethod, reader);
        factory = new(suspenderType, writerParameterType, writer, resumerDescriptor);
        return true;

        bool HasSuspendMethod(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters is not [ParameterInfo stateMachineParam, ParameterInfo suspensionContextParam, ParameterInfo writerParameter])
                return false;

            if (!stateMachineParam.ParameterType.IsByRef || stateMachineParam.ParameterType.GetElementType() != stateMachineType)
                return false;

            if (suspensionContextParam.ParameterType != typeof(IDehydrationContext))
                return false;

            if (method.ReturnType != typeof(void))
                return false;

            if (hasSuspendMethod)
                return false;

            writerParameterType = writerParameter.ParameterType;
            hasSuspendMethod = true;
            return true;
        }

        bool HasResumeWithVoidMethod(MethodInfo method, ParameterInfo[] parameters)
        {
            Debug.Assert(parameters.Length == 1);

            if (method.ReturnType != typeof(IDAsyncRunnable))
                return false;

            if (resumeWithVoidMethod is not null)
                return false;

            if (readerParameterType is null)
            {
                readerParameterType = parameters[0].ParameterType;
            }
            else if (readerParameterType != parameters[0].ParameterType)
                return false;

            resumeWithVoidMethod = method;
            return true;
        }

        bool HasResumeWithResultMethod(MethodInfo method, ParameterInfo[] parameters)
        {
            Debug.Assert(parameters.Length == 2);

            if (!method.IsGenericMethodDefinition)
                return false;

            if (method.GetGenericArguments() is not [Type resultType])
                return false;

            if (parameters[1].ParameterType != resultType)
                return false;

            if (method.ReturnType != typeof(IDAsyncRunnable))
                return false;

            if (resumeWithResultMethod is not null)
                return false;

            if (readerParameterType is null)
            {
                readerParameterType = parameters[0].ParameterType;
            }
            else if (readerParameterType != parameters[0].ParameterType)
                return false;

            resumeWithResultMethod = method;
            return true;
        }

        bool HasResumeWithExceptionMethod(MethodInfo method, ParameterInfo[] parameters)
        {
            Debug.Assert(parameters.Length == 2);

            if (parameters[1].ParameterType != typeof(Exception))
                return false;

            if (method.ReturnType != typeof(IDAsyncRunnable))
                return false;

            if (resumeWithExceptionMethod is not null)
                return false;

            if (readerParameterType is null)
            {
                readerParameterType = parameters[0].ParameterType;
            }
            else if (readerParameterType != parameters[0].ParameterType)
                return false;

            resumeWithExceptionMethod = method;
            return true;
        }

        static bool False(out ConverterDescriptorFactory? factory)
        {
            factory = null;
            return false;
        }
    }
}
