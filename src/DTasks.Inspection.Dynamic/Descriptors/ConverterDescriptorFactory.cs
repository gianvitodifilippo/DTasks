using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Reflection;
using DTasks.Hosting;
using DTasks.Marshaling;
using DTasks.Utils;

namespace DTasks.Inspection.Dynamic.Descriptors;

internal class ConverterDescriptorFactory(
    Type converterGenericType,
    Type readerParameterType,
    Type writerParameterType,
    IReaderDescriptor reader,
    IWriterDescriptor writer) : IConverterDescriptorFactory
{
    public IConverterDescriptor CreateDescriptor(Type stateMachineType)
    {
        Type converterType = converterGenericType.MakeGenericType(stateMachineType);

        MethodInfo suspendMethod = converterType.GetRequiredMethod(
            name: InspectionConstants.SuspendMethodName,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [stateMachineType.MakeByRefType(), typeof(ISuspensionContext), writerParameterType]);

        MethodInfo resumeWithVoidMethod = converterType.GetRequiredMethod(
            name: InspectionConstants.ResumeMethodName,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [readerParameterType]);

        MethodInfo resumeWithResultMethod = converterType.GetRequiredMethod(
            name: InspectionConstants.ResumeMethodName,
            bindingAttr: BindingFlags.Instance | BindingFlags.Public,
            parameterTypes: [readerParameterType, Type.MakeGenericMethodParameter(0)]);

        return new ConverterDescriptor(converterType, suspendMethod, resumeWithVoidMethod, resumeWithResultMethod, reader, writer);
    }

    public static bool TryCreate(Type converterType, [NotNullWhen(true)] out ConverterDescriptorFactory? factory)
    {
        if (!converterType.IsInterface)
            return False(out factory);

        if (!converterType.IsGenericTypeDefinition)
            return False(out factory);

        if (converterType.GetGenericArguments() is not [Type stateMachineType])
            return False(out factory);

        if (stateMachineType.BaseType != typeof(object))
            return False(out factory);

        if (stateMachineType.GetInterfaces() is not [])
            return False(out factory);

        if (stateMachineType.GetGenericParameterConstraints() is not [])
            return False(out factory);

        MethodInfo[] methods = converterType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        bool hasSuspendMethod = false;
        bool hasResumeWithVoidMethod = false;
        bool hasResumeWithResultMethod = false;
        Type? writerParameterType = null;
        Type? readerParameterType = null;

        foreach (MethodInfo method in methods)
        {
            ParameterInfo[] parameters = method.GetParameters();
            switch (method.Name)
            {
                case InspectionConstants.SuspendMethodName:
                    if (!HasSuspendMethod(method, parameters))
                        return False(out factory);
                    break;

                case InspectionConstants.ResumeMethodName:
                    switch (parameters.Length)
                    {
                        case 1:
                            if (!HasResumeWithVoidMethod(method, parameters))
                                return False(out factory);
                            break;

                        case 2:
                            if (!HasResumeWithResultMethod(method, parameters))
                                return False(out factory);
                            break;

                        default:
                            return False(out factory);
                    }
                    break;

                default:
                    return False(out factory);
            }
        }

        if (!hasSuspendMethod || !hasResumeWithVoidMethod || !hasResumeWithResultMethod)
            return False(out factory);

        if (readerParameterType is null || writerParameterType is null)
            return False(out factory);

        Type readerType = readerParameterType.IsByRef
            ? readerParameterType.GetElementType()
            : readerParameterType;

        if (!readerType.IsValueType && readerParameterType.IsByRef)
            return False(out factory);

        Type writerType = writerParameterType.IsByRef
            ? writerParameterType.GetElementType()
            : writerParameterType;

        if (!writerType.IsValueType && writerParameterType.IsByRef)
            return False(out factory);

        if (!ReaderDescriptor.TryCreate(readerType, out ReaderDescriptor? reader))
            return False(out factory);

        if (!WriterDescriptor.TryCreate(writerType, out WriterDescriptor? writer))
            return False(out factory);

        factory = new(converterType, readerParameterType, writerParameterType, reader, writer);
        return true;

        bool HasSuspendMethod(MethodInfo method, ParameterInfo[] parameters)
        {
            if (parameters is not [ParameterInfo stateMachineParam, ParameterInfo suspensionContextParam, ParameterInfo writerParameter])
                return false;

            if (!stateMachineParam.ParameterType.IsByRef || stateMachineParam.ParameterType.GetElementType() != stateMachineType)
                return false;

            if (suspensionContextParam.ParameterType != typeof(ISuspensionContext))
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

            if (hasResumeWithVoidMethod)
                return false;

            if (readerParameterType is null)
            {
                readerParameterType = parameters[0].ParameterType;
            }
            else if (readerParameterType != parameters[0].ParameterType)
                return false;

            hasResumeWithVoidMethod = true;
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

            if (hasResumeWithResultMethod)
                return false;

            if (readerParameterType is null)
            {
                readerParameterType = parameters[0].ParameterType;
            }
            else if (readerParameterType != parameters[0].ParameterType)
                return false;

            hasResumeWithResultMethod = true;
            return true;
        }

        static bool False(out ConverterDescriptorFactory? factory)
        {
            factory = null;
            return false;
        }
    }
}
