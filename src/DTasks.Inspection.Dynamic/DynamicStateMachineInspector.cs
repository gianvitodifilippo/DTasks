using DTasks.Inspection.Dynamic.Descriptors;
using DTasks.Marshaling;
using DTasks.Utils;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace DTasks.Inspection.Dynamic;

using IndexerAwaiterField = (FieldInfo Field, int Index);

public sealed class DynamicStateMachineInspector : IStateMachineInspector
{
    private static readonly ConstructorInfo s_objectConstructor = typeof(object).GetRequiredConstructor(
        bindingAttr: BindingFlags.Instance | BindingFlags.Public,
        parameterTypes: []);

    private readonly IConverterDescriptorFactory _converterDescriptorFactory;
    private readonly ITypeResolver _typeResolver;
    private readonly ConcurrentDictionary<Type, object> _converters;
    private readonly DynamicAssembly _assembly;

    internal DynamicStateMachineInspector(IConverterDescriptorFactory converterDescriptorFactory, ITypeResolver typeResolver)
    {
        _converterDescriptorFactory = converterDescriptorFactory;
        _typeResolver = typeResolver;
        _converters = [];
        _assembly = new();
    }

    public object GetConverter(Type stateMachineType)
    {
        return _converters.GetOrAdd(stateMachineType, CreateConverter, this);

        static object CreateConverter(Type stateMachineType, DynamicStateMachineInspector self)
            => self.CreateConverter(stateMachineType);
    }

    private object CreateConverter(Type stateMachineType)
    {
        FieldInfo[] fields = stateMachineType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        ConstructorInfo? stateMachineConstructor = stateMachineType.GetConstructor([]);
        if (stateMachineConstructor is null && !stateMachineType.IsValueType)
            throw new ArgumentException("The state machine type should have a parameterless constructor.", nameof(stateMachineType));

        IConverterDescriptor converterDescriptor = _converterDescriptorFactory.CreateDescriptor(stateMachineType);
        TypeBuilder converterType = _assembly.DefineConverterType(stateMachineType);
        converterType.AddInterfaceImplementation(converterDescriptor.Type);

        FieldBuilder typeResolverField = DefineTypeResolverField(converterType);
        DefineConstructor(converterType, typeResolverField);
        
        MethodBuilder suspendMethod = DefineMethodOverride(converterType, converterDescriptor.SuspendMethod);
        MethodBuilder resumeWithVoidMethod = DefineMethodOverride(converterType, converterDescriptor.ResumeWithVoidMethod);
        MethodBuilder resumeWithResultMethod = DefineMethodOverride(converterType, converterDescriptor.ResumeWithResultMethod);

        SuspendMethodImplementor suspendImplementor = new(suspendMethod, stateMachineType, converterDescriptor);
        ResumeWithVoidMethodImplementor resumeWithVoidImplementor = new(resumeWithVoidMethod, stateMachineType, converterDescriptor);
        ResumeWithResultMethodImplementor resumeWithResultImplementor = new(resumeWithResultMethod, stateMachineType, converterDescriptor);

        List<IndexerAwaiterField> dAsyncAwaiterFields = [];
        int awaiterIndex = 0;
        foreach (FieldInfo field in fields)
        {
            StateMachineFieldKind fieldKind = StateMachineFacts.GetFieldKind(field);

            switch (fieldKind)
            {
                case StateMachineFieldKind.UserField:
                case StateMachineFieldKind.StateField:
                    suspendImplementor.OnUserField(field);
                    break;

                case StateMachineFieldKind.DAsyncAwaiterField:
                    if (field.FieldType.IsValueType)
                    {
                        dAsyncAwaiterFields.Add((field, awaiterIndex));
                        awaiterIndex++;
                    }
                    else
                    {
                        if (field.FieldType != typeof(object))
                            throw new NotSupportedException("The provided state machine layout is not supported.");
                        
                        dAsyncAwaiterFields.Add((field, -1));
                    }

                    break;
            }
        }

        suspendImplementor.OnAwaiterFields(dAsyncAwaiterFields, typeResolverField);

        suspendImplementor.Return();
        resumeWithVoidImplementor.Return();
        resumeWithResultImplementor.Return();

        return Activator.CreateInstance(converterType.CreateTypeInfo(), [_typeResolver]);
    }

    private static FieldBuilder DefineTypeResolverField(TypeBuilder converterType)
    {
        return converterType.DefineField("_typeResolver", typeof(ITypeResolver), FieldAttributes.Private | FieldAttributes.InitOnly);
    }

    private static ConstructorBuilder DefineConstructor(TypeBuilder converterType, FieldInfo typeResolverField)
    {
        ConstructorBuilder constructor = converterType.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, [typeof(ITypeResolver)]);
        constructor.DefineParameter(
            iSequence: 1,
            attributes: ParameterAttributes.None,
            strParamName: "typeBuilder");
        
        ILGenerator il = constructor.GetILGenerator();

        il.Emit(OpCodes.Ldarg_0);                   // Stack: this
        il.Emit(OpCodes.Ldarg_1);                   // Stack: this, $typeBuilder
        il.Emit(OpCodes.Stfld, typeResolverField);  // Stack: -
        il.Emit(OpCodes.Ldarg_0);                   // Stack: this
        il.Emit(OpCodes.Call, s_objectConstructor); // Stack: -
        il.Emit(OpCodes.Ret);                       // Stack: -

        return constructor;
    }

    private static MethodBuilder DefineMethodOverride(TypeBuilder converterType, MethodInfo declaration)
    {
        ParameterInfo[] parameters = declaration.GetParameters();

        MethodBuilder method = converterType.DefineMethod(
            name: declaration.Name,
            attributes: MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final,
            callingConvention: CallingConventions.HasThis,
            returnType: declaration.ReturnType,
            returnTypeRequiredCustomModifiers: declaration.ReturnParameter.GetRequiredCustomModifiers(),
            returnTypeOptionalCustomModifiers: declaration.ReturnParameter.GetOptionalCustomModifiers(),
            parameterTypes: parameters.Map(parameter => parameter.ParameterType),
            parameterTypeRequiredCustomModifiers: parameters.Map(parameter => parameter.GetRequiredCustomModifiers()),
            parameterTypeOptionalCustomModifiers: parameters.Map(parameter => parameter.GetOptionalCustomModifiers()));

        if (declaration.IsGenericMethodDefinition)
        {
            Type[] genericParameterTypes = declaration.GetGenericArguments();
            GenericTypeParameterBuilder[] genericTypeParameters = method.DefineGenericParameters(genericParameterTypes.Map(type => type.Name));
            
            Debug.Assert(genericParameterTypes.Length == genericTypeParameters.Length);
            for (int i = 0; i < genericParameterTypes.Length; i++)
            {
                Type genericParameterType = genericParameterTypes[i];
                var genericTypeParameter = genericTypeParameters[i];

                if (genericParameterType.BaseType is Type baseType && baseType != typeof(object))
                {
                    genericTypeParameter.SetBaseTypeConstraint(baseType);
                }

                genericTypeParameter.SetGenericParameterAttributes(genericParameterType.GenericParameterAttributes);

                if (genericParameterType.GetInterfaces() is { Length: > 0 } interfaceTypes)
                {
                    genericTypeParameter.SetInterfaceConstraints(interfaceTypes);
                }
            }
        }

        foreach (ParameterInfo parameter in parameters)
        {
            method.DefineParameter(
                position: parameter.Position + 1,
                attributes: parameter.Attributes,
                strParamName: parameter.Name);
        }

        converterType.DefineMethodOverride(method, declaration);

        return method;
    }

    public static DynamicStateMachineInspector Create(Type converterType, ITypeResolver typeResolver)
    {
        if (!ConverterDescriptorFactory.TryCreate(converterType, out ConverterDescriptorFactory? converterDescriptorFactory))
            throw new ArgumentException("The provided converter type is not compliant.", nameof(converterType));

        return new(converterDescriptorFactory, typeResolver);
    }

    private readonly ref struct SuspendMethodImplementor(MethodBuilder suspendMethod, Type stateMachineType, IConverterDescriptor converterDescriptor)
    {
        private readonly InspectorILGenerator _il = InspectorILGenerator.Create(
            suspendMethod,
            stateMachineType,
            converterDescriptor.SuspendMethod.GetParameters()[^1].ParameterType,
            converterDescriptor.Writer.Type);

        public void OnUserField(FieldInfo field)
        {
            MethodInfo writeFieldMethod = converterDescriptor.Writer.GetWriteFieldMethod(field.FieldType);

            // writer.WriteField($fieldName, stateMachine.$field);
            _il.LoadWriter();                       // Stack: writer
            _il.LoadString(field.Name);             // Stack: writer, $fieldName
            _il.LoadStateMachineArg();              // Stack: writer, $fieldName, stateMachine
            _il.LoadField(field);                   // Stack: writer, $fieldName, stateMachine.$field
            _il.CallWriterMethod(writeFieldMethod); // Stack: -
        }

        public void OnAwaiterFields(List<IndexerAwaiterField> indexedFields, FieldInfo typeResolverField)
        {
            Label ifFalseLabel = default;
            bool wasLabelDefined = false;
            foreach ((FieldInfo field, int index) in indexedFields)
            {
                MethodInfo writeFieldMethod = converterDescriptor.Writer.GetWriteFieldMethod(typeof(string));

                if (wasLabelDefined)
                {
                    // return;
                    _il.Return();                // Stack: -
                    _il.MarkLabel(ifFalseLabel); // Stack: -
                }

                ifFalseLabel = _il.DefineLabel();
                wasLabelDefined = true;

                // if (suspensionContext.IsSuspended(ref stateMachine.$field))
                _il.LoadSuspensionContext();                // Stack: suspensionContext
                _il.LoadStateMachineArg();                  // Stack: suspensionContext, stateMachine
                _il.LoadFieldAddress(field);                // Stack: suspensionContext, &stateMachine.$field
                _il.CallIsSuspendedMethod(field.FieldType); // Stack: @result[suspensionContext.IsSuspended(&stateMachine.$field)]
                _il.BranchIfFalse(ifFalseLabel);            // Stack: -

                if (field.FieldType.IsValueType)
                {
                    // writer.WriteField($InspectionConstants.AwaiterFieldName, $field.Name);
                    _il.LoadWriter();                                     // Stack: writer
                    _il.LoadString(InspectionConstants.AwaiterFieldName); // Stack: writer, $InspectionConstants.AwaiterFieldName
                    _il.LoadString(index.ToString());                     // Stack: writer, $InspectionConstants.AwaiterFieldName, $index.ToString()
                    _il.CallWriterMethod(writeFieldMethod);               // Stack: -
                }
                else
                {
                    // writer.WriteField($InspectionConstants.AwaiterFieldName, _typeResolver.GetTypeId(stateMachine.$field.GetType()).Value);
                    _il.LoadWriter();                                     // Stack: writer
                    _il.LoadString(InspectionConstants.AwaiterFieldName); // Stack: writer, $InspectionConstants.AwaiterFieldName
                    _il.LoadThis();                                       // Stack: writer, $InspectionConstants.AwaiterFieldName, this
                    _il.LoadField(typeResolverField);                     // Stack: writer, $InspectionConstants.AwaiterFieldName, _typeResolver
                    _il.LoadStateMachineArg();                            // Stack: writer, $InspectionConstants.AwaiterFieldName, _typeResolver, stateMachine
                    _il.LoadField(field);                                 // Stack: writer, $InspectionConstants.AwaiterFieldName, _typeResolver, stateMachine.$field
                    _il.CallGetType();                                    // Stack: writer, $InspectionConstants.AwaiterFieldName, _typeResolver, awaiterType=@result[stateMachine.$field.GetType()]
                    _il.CallGetTypeId();                                  // Stack: writer, $InspectionConstants.AwaiterFieldName, _typeResolver, typeId=@result[_typeResolver.GetTypeId(awaiterType)]
                    _il.GetTypeIdValue();                                 // Stack: writer, $InspectionConstants.AwaiterFieldName, _typeResolver, @result[typeId.Value]
                    _il.CallWriterMethod(writeFieldMethod);               // Stack: -
                }
            }

            if (wasLabelDefined) // check in case there are no awaiters
            {
                _il.MarkLabel(ifFalseLabel);
            }
        }

        public void Return()
        {
            _il.Return(); // Stack: -
        }
    }

    private readonly ref struct ResumeWithVoidMethodImplementor(MethodBuilder resumeWithVoidMethod, Type stateMachineType, IConverterDescriptor converterDescriptor)
    {
        private readonly InspectorILGenerator _il = InspectorILGenerator.Create(
            resumeWithVoidMethod,
            stateMachineType,
            converterDescriptor.ResumeWithVoidMethod.GetParameters()[0].ParameterType,
            converterDescriptor.Reader.Type);

        public void Return()
        {
            _il.Return(); // Stack: -
        }
    }

    private readonly ref struct ResumeWithResultMethodImplementor(MethodBuilder resumeWithResultMethod, Type stateMachineType, IConverterDescriptor converterDescriptor)
    {
        private readonly InspectorILGenerator _il = InspectorILGenerator.Create(
            resumeWithResultMethod,
            stateMachineType,
            converterDescriptor.ResumeWithResultMethod.GetParameters()[0].ParameterType,
            converterDescriptor.Reader.Type);

        public void Return()
        {
            _il.Return(); // Stack: -
        }
    }
}
